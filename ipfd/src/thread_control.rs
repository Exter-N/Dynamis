use std::{
    collections::{hash_map::Entry, HashMap},
    mem::size_of,
    ops::Deref,
};

use windows::{
    core::{Error, Owned, Result},
    Win32::{
        Foundation::{CloseHandle, ERROR_NO_MORE_FILES, HANDLE},
        System::{
            Diagnostics::ToolHelp::*,
            Threading::{
                GetCurrentProcessId, GetCurrentThreadId, OpenThread, ResumeThread, SuspendThread,
                THREAD_ACCESS_RIGHTS, THREAD_SUSPEND_RESUME,
            },
        },
    },
};

struct ThreadIter {
    snapshot: Owned<HANDLE>,
    current: THREADENTRY32,
}

impl ThreadIter {
    fn new(process_id: u32) -> Result<Self> {
        let snapshot =
            unsafe { Owned::new(CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD, process_id)?) };
        Ok(Self {
            snapshot,
            current: Default::default(),
        })
    }
    fn next(&mut self) -> Result<bool> {
        let result = if self.current.dwSize == 0 {
            self.current.dwSize = size_of::<THREADENTRY32>() as u32;
            unsafe { Thread32First(*self.snapshot, &mut self.current) }
        } else {
            unsafe { Thread32Next(*self.snapshot, &mut self.current) }
        };
        match result {
            Ok(_) => Ok(true),
            Err(e) => {
                if e.code() == ERROR_NO_MORE_FILES.to_hresult() {
                    Ok(false)
                } else {
                    Err(e)
                }
            }
        }
    }
}

pub struct SuspendedThread {
    handle: HANDLE,
}

impl SuspendedThread {
    pub fn new(thread_id: u32, desired_access: THREAD_ACCESS_RIGHTS) -> Result<Self> {
        let handle =
            unsafe { OpenThread(THREAD_SUSPEND_RESUME | desired_access, false, thread_id)? };
        if unsafe { SuspendThread(handle) } == u32::MAX {
            let error = Error::from_win32();
            let _ = unsafe { CloseHandle(handle) };
            Err(error)
        } else {
            Ok(Self { handle })
        }
    }
}

impl Deref for SuspendedThread {
    type Target = HANDLE;

    fn deref(&self) -> &Self::Target {
        &self.handle
    }
}

impl Drop for SuspendedThread {
    fn drop(&mut self) {
        unsafe {
            ResumeThread(self.handle);
            let _ = CloseHandle(self.handle);
        }
    }
}

pub fn map_threads<T>(mut f: impl FnMut(u32) -> Result<T>) -> Result<HashMap<u32, T>> {
    let current_pid = unsafe { GetCurrentProcessId() };
    let current_tid = unsafe { GetCurrentThreadId() };
    let mut visited_threads: HashMap<u32, T> = HashMap::new();
    loop {
        let mut found_new_threads = false;
        let mut thread_iter = ThreadIter::new(current_pid)?;
        while thread_iter.next()? {
            let thread_id = thread_iter.current.th32ThreadID;
            if thread_id == current_tid {
                continue;
            }
            if let Entry::Vacant(entry) = visited_threads.entry(thread_id) {
                entry.insert(f(thread_id)?);
                found_new_threads = true;
            }
        }
        drop(thread_iter);
        if !found_new_threads {
            break;
        }
    }
    Ok(visited_threads)
}
