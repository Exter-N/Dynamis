use std::{
    ffi::c_void,
    mem::transmute,
    ptr::null_mut,
    sync::atomic::{AtomicUsize, Ordering},
};

use windows::{
    core::{w, Error, Result},
    Win32::{
        Foundation::{EXCEPTION_BREAKPOINT, EXCEPTION_SINGLE_STEP},
        System::{
            Diagnostics::Debug::{
                AddVectoredExceptionHandler, GetThreadContext, RemoveVectoredExceptionHandler,
                SetThreadContext, CONTEXT, CONTEXT_DEBUG_REGISTERS_AMD64,
                EXCEPTION_CONTINUE_EXECUTION, EXCEPTION_CONTINUE_SEARCH, EXCEPTION_POINTERS,
                PVECTORED_EXCEPTION_HANDLER,
            },
            Memory::{VirtualQuery, MEMORY_BASIC_INFORMATION, MEM_IMAGE},
            Threading::{
                GetCurrentThread, GetCurrentThreadId, SetThreadDescription, THREAD_GET_CONTEXT,
                THREAD_SET_CONTEXT,
            },
        },
    },
};

use crate::thread_control::{map_threads, SuspendedThread};

pub static BREAKPOINT_CALLBACK: AtomicUsize = AtomicUsize::new(0);

#[derive(Clone, Copy, Default, Debug)]
pub struct Breakpoint {
    pub flags: u8,
    pub address: usize,
}

fn context_set_breakpoints(context: &mut CONTEXT, breakpoints: [Option<Breakpoint>; 4]) {
    let mut any_enable_flags = 0u64;
    if let Some(breakpoint) = breakpoints[0] {
        let enable_flags = (breakpoint.flags & 3) as u64;
        let cond_len_flags = ((breakpoint.flags & 0xF0) >> 4) as u64;
        any_enable_flags |= enable_flags;
        context.Dr0 = breakpoint.address as u64;
        context.Dr7 = (context.Dr7 & !0xF_0003) | enable_flags | (cond_len_flags << 16);
    }
    if let Some(breakpoint) = breakpoints[1] {
        let enable_flags = (breakpoint.flags & 3) as u64;
        let cond_len_flags = ((breakpoint.flags & 0xF0) >> 4) as u64;
        any_enable_flags |= enable_flags;
        context.Dr1 = breakpoint.address as u64;
        context.Dr7 = (context.Dr7 & !0xF0_000C) | (enable_flags << 2) | (cond_len_flags << 20);
    }
    if let Some(breakpoint) = breakpoints[2] {
        let enable_flags = (breakpoint.flags & 3) as u64;
        let cond_len_flags = ((breakpoint.flags & 0xF0) >> 4) as u64;
        any_enable_flags |= enable_flags;
        context.Dr2 = breakpoint.address as u64;
        context.Dr7 = (context.Dr7 & !0xF00_0030) | (enable_flags << 4) | (cond_len_flags << 24);
    }
    if let Some(breakpoint) = breakpoints[3] {
        let enable_flags = (breakpoint.flags & 3) as u64;
        let cond_len_flags = ((breakpoint.flags & 0xF0) >> 4) as u64;
        any_enable_flags |= enable_flags;
        context.Dr3 = breakpoint.address as u64;
        context.Dr7 = (context.Dr7 & !0xF000_00C0) | (enable_flags << 6) | (cond_len_flags << 28);
    }
    context.Dr7 |= any_enable_flags << 8;
}

pub fn process_set_breakpoints(breakpoints: [Option<Breakpoint>; 4]) -> Result<()> {
    let mut context: CONTEXT = Default::default();
    context.ContextFlags = CONTEXT_DEBUG_REGISTERS_AMD64;
    map_threads(|thread_id| {
        if let Ok(thread) = SuspendedThread::new(thread_id, THREAD_GET_CONTEXT | THREAD_SET_CONTEXT)
        {
            if unsafe { GetThreadContext(*thread, &mut context) }.is_err() {
                return Ok(());
            }
            context_set_breakpoints(&mut context, breakpoints);
            let _ = unsafe { SetThreadContext(*thread, &context) };
        }
        Ok(())
    })?;
    Ok(())
}

pub fn thread_set_breakpoints(thread_id: u32, breakpoints: [Option<Breakpoint>; 4]) -> Result<()> {
    if thread_id == unsafe { GetCurrentThreadId() } {
        return Ok(());
    }
    let thread = SuspendedThread::new(thread_id, THREAD_GET_CONTEXT | THREAD_SET_CONTEXT)?;
    let mut context: CONTEXT = Default::default();
    context.ContextFlags = CONTEXT_DEBUG_REGISTERS_AMD64;
    unsafe { GetThreadContext(*thread, &mut context)? };
    context_set_breakpoints(&mut context, breakpoints);
    unsafe { SetThreadContext(*thread, &context) }
}

pub fn current_thread_set_description() {
    let _ = unsafe { SetThreadDescription(GetCurrentThread(), w!("Dynamis IPFD server thread")) };
}

pub struct Veh {
    handle: *mut c_void,
}

impl Veh {
    fn new(first: bool, handler: PVECTORED_EXCEPTION_HANDLER) -> Result<Veh> {
        let handle = unsafe { AddVectoredExceptionHandler(if first { 1 } else { 0 }, handler) };
        if handle == null_mut() {
            return Err(Error::from_win32());
        }
        Ok(Self { handle })
    }
}

impl Drop for Veh {
    fn drop(&mut self) {
        unsafe {
            RemoveVectoredExceptionHandler(self.handle);
        }
    }
}

fn is_in_dynamic_code(exceptioninfo: *const EXCEPTION_POINTERS) -> Option<bool> {
    let rip = unsafe { (*(*exceptioninfo).ContextRecord).Rip };
    let mut buffer: MEMORY_BASIC_INFORMATION = Default::default();
    let size = size_of::<MEMORY_BASIC_INFORMATION>();
    if unsafe { VirtualQuery(Some(rip as *const c_void), &mut buffer, size) } < size {
        return None;
    }
    Some(buffer.Type != MEM_IMAGE)
}

fn get_breakpoint_callback() -> PVECTORED_EXCEPTION_HANDLER {
    let raw_callback = BREAKPOINT_CALLBACK.load(Ordering::SeqCst);
    if raw_callback != 0 {
        unsafe { Some(transmute(raw_callback)) }
    } else {
        None
    }
}

extern "system" fn handle_exception(exceptioninfo: *mut EXCEPTION_POINTERS) -> i32 {
    let code = unsafe { (*(*exceptioninfo).ExceptionRecord).ExceptionCode };
    if code != EXCEPTION_BREAKPOINT && code != EXCEPTION_SINGLE_STEP {
        return EXCEPTION_CONTINUE_SEARCH;
    }

    let action = if is_in_dynamic_code(exceptioninfo).unwrap_or(true) {
        EXCEPTION_CONTINUE_EXECUTION
    } else if let Some(callback) = get_breakpoint_callback() {
        unsafe { callback(exceptioninfo) }
    } else {
        EXCEPTION_CONTINUE_EXECUTION
    };

    if action == EXCEPTION_CONTINUE_EXECUTION {
        unsafe {
            let context = (*exceptioninfo).ContextRecord;
            (*context).Dr6 &= !0xF;
            (*context).EFlags |= 0x1000;
        }
    }

    action
}

pub fn create_veh() -> Result<Veh> {
    Veh::new(true, Some(handle_exception))
}
