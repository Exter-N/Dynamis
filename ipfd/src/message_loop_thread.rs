use std::mem::take;
use std::os::windows::io::AsRawHandle;
use std::panic::resume_unwind;
use std::sync::mpsc::{channel, Sender};
use std::thread::{self, JoinHandle};

use windows::Win32::Foundation::HANDLE;
use windows::Win32::System::Threading::GetThreadId;

pub struct MessageLoopThread<Message: Send + 'static> {
    thread: Option<JoinHandle<()>>,
    sender: Sender<Option<Message>>,
}

pub trait MessageServer<Message: Send + 'static> {
    fn handle(&mut self, message: Message);
}

impl<Message: Send + 'static> MessageLoopThread<Message> {
    pub fn new<F: MessageServer<Message>>(
        server_factory: impl FnOnce() -> F + Send + 'static,
    ) -> Self {
        let (sender, receiver) = channel::<Option<Message>>();
        let thread = thread::spawn(move || {
            let mut server = server_factory();
            while let Ok(maybe_message) = receiver.recv() {
                match maybe_message {
                    Some(message) => {
                        server.handle(message);
                    }
                    None => {
                        break;
                    }
                }
            }
        });
        Self {
            thread: Some(thread),
            sender,
        }
    }
    pub fn send(&self, message: Message) {
        self.sender.send(Some(message)).unwrap()
    }
    pub fn thread_id(&self) -> u32 {
        match &self.thread {
            Some(thread) => unsafe { GetThreadId(HANDLE(thread.as_raw_handle())) },
            None => 0,
        }
    }
}

impl<Message: Send> Drop for MessageLoopThread<Message> {
    fn drop(&mut self) {
        let _ = self.sender.send(None);
        if let Some(thread) = take(&mut self.thread) {
            match thread.join() {
                Ok(_) => {}
                Err(e) => resume_unwind(e),
            }
        }
    }
}
