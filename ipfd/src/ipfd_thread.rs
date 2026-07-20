use std::{ffi::c_void, ptr::copy};

use windows::{
    core::w,
    Win32::{
        Foundation::HANDLE,
        System::Threading::{GetCurrentThread, ReleaseSemaphore, SetEvent, SetThreadDescription},
    },
};

use crate::{
    ipfd_impl::{self, Breakpoint},
    message_loop_thread::{MessageLoopThread, MessageServer},
};

pub struct IpfdThread {
    thread: MessageLoopThread<IpfdMessage>,
}

#[derive(Debug)]
pub enum IpfdMessage {
    SetBreakpoint {
        index: u8,
        breakpoint: Breakpoint,
    },
    RefreshAllBreakpoints,
    ClearAllBreakpoints,
    MemoryCopy {
        source: usize,
        destination: usize,
        size: usize,
    },
    ThreadRefreshBreakpoints {
        thread_id: u32,
    },
    SetEvent {
        hevent: usize,
    },
    ReleaseSemaphore {
        hsemaphore: usize,
        release_count: i32,
    },
}

struct IpfdServer {
    breakpoints: [Option<Breakpoint>; 4],
    user_thread_id: u32,
    _veh: ipfd_impl::Veh,
}

impl IpfdServer {
    fn new(user_thread_id: u32) -> Self {
        let _ =
            unsafe { SetThreadDescription(GetCurrentThread(), w!("Dynamis IPFD server thread")) };
        Self {
            breakpoints: [None; 4],
            user_thread_id,
            _veh: ipfd_impl::create_veh().unwrap(),
        }
    }
}

impl MessageServer<IpfdMessage> for IpfdServer {
    fn handle(&mut self, message: IpfdMessage) {
        match message {
            IpfdMessage::SetBreakpoint { index, breakpoint } => {
                self.breakpoints[index as usize] = Some(breakpoint);
                let mut breakpoints = [None; 4];
                breakpoints[index as usize] = Some(breakpoint);
                ipfd_impl::process_set_breakpoints(breakpoints, &[self.user_thread_id]).unwrap()
            }
            IpfdMessage::RefreshAllBreakpoints => {
                ipfd_impl::process_set_breakpoints(self.breakpoints, &[self.user_thread_id])
                    .unwrap()
            }
            IpfdMessage::ClearAllBreakpoints => {
                self.breakpoints = [Some(Default::default()); 4];
                ipfd_impl::process_set_breakpoints(
                    [Some(Default::default()); 4],
                    &[self.user_thread_id],
                )
                .unwrap()
            }
            IpfdMessage::MemoryCopy {
                source,
                destination,
                size,
            } => unsafe { copy(source as *const u8, destination as *mut u8, size) },
            IpfdMessage::ThreadRefreshBreakpoints { thread_id } => {
                if thread_id != self.user_thread_id {
                    ipfd_impl::thread_set_breakpoints(thread_id, self.breakpoints).unwrap()
                }
            }
            IpfdMessage::SetEvent { hevent } => {
                unsafe { SetEvent(HANDLE(hevent as *mut c_void)) }.unwrap()
            }
            IpfdMessage::ReleaseSemaphore {
                hsemaphore,
                release_count,
            } => {
                unsafe { ReleaseSemaphore(HANDLE(hsemaphore as *mut c_void), release_count, None) }
                    .unwrap()
            }
        }
    }
}

impl Drop for IpfdServer {
    fn drop(&mut self) {
        let _ = ipfd_impl::process_set_breakpoints(
            [Some(Default::default()); 4],
            &[self.user_thread_id],
        );
    }
}

impl IpfdThread {
    pub fn new(user_thread_id: u32) -> Self {
        let result = Self {
            thread: MessageLoopThread::new(move || IpfdServer::new(user_thread_id)),
        };
        result
    }
    pub fn send(&self, message: IpfdMessage) {
        self.thread.send(message)
    }
}
