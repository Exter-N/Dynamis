use std::mem::take;
use std::panic::resume_unwind;
use std::sync::mpsc::{channel, Sender};
use std::thread::{self, JoinHandle};

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
