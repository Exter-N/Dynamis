use std::{ffi::c_void, mem::transmute, ptr::copy};

use windows::{
    core::w,
    Win32::{
        Foundation::HANDLE,
        System::Threading::{GetCurrentThread, ReleaseSemaphore, SetEvent, SetThreadDescription},
    },
};

use crate::message_loop_thread::{MessageLoopThread, MessageServer};

type UserCallback =
    Option<unsafe extern "C" fn(rcx: usize, rdx: usize, r8: usize, r9: usize) -> usize>;

pub struct IpfdUserThread {
    thread: MessageLoopThread<IpfdUserMessage>,
}

#[derive(Debug)]
pub enum IpfdUserMessage {
    Invoke {
        function: usize,
        arg0: usize,
        arg1: usize,
        return_ptr: usize,
    },
    MemoryCopy {
        source: usize,
        destination: usize,
        size: usize,
    },
    SetEvent {
        hevent: usize,
    },
    ReleaseSemaphore {
        hsemaphore: usize,
        release_count: i32,
    },
}

struct IpfdUserServer {}

impl IpfdUserServer {
    fn new() -> Self {
        let _ = unsafe { SetThreadDescription(GetCurrentThread(), w!("Dynamis IPFD user thread")) };
        Self {}
    }
}

impl MessageServer<IpfdUserMessage> for IpfdUserServer {
    fn handle(&mut self, message: IpfdUserMessage) {
        match message {
            IpfdUserMessage::Invoke {
                function,
                arg0,
                arg1,
                return_ptr,
            } => unsafe {
                if let Some(f) = transmute::<usize, UserCallback>(function) {
                    let ret = f(arg0, arg1, 0, 0);
                    if return_ptr != 0 {
                        *(return_ptr as *mut usize) = ret;
                    }
                }
            },
            IpfdUserMessage::MemoryCopy {
                source,
                destination,
                size,
            } => unsafe { copy(source as *const u8, destination as *mut u8, size) },
            IpfdUserMessage::SetEvent { hevent } => {
                unsafe { SetEvent(HANDLE(hevent as *mut c_void)) }.unwrap()
            }
            IpfdUserMessage::ReleaseSemaphore {
                hsemaphore,
                release_count,
            } => {
                unsafe { ReleaseSemaphore(HANDLE(hsemaphore as *mut c_void), release_count, None) }
                    .unwrap()
            }
        }
    }
}

impl IpfdUserThread {
    pub fn new() -> Self {
        Self {
            thread: MessageLoopThread::new(|| IpfdUserServer::new()),
        }
    }
    pub fn thread_id(&self) -> u32 {
        self.thread.thread_id()
    }
    pub fn send(&self, message: IpfdUserMessage) {
        self.thread.send(message)
    }
}
