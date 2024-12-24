mod ipfd_impl;
mod ipfd_thread;
mod message_loop_thread;
mod thread_control;

use std::{
    mem::forget,
    sync::{
        atomic::{AtomicUsize, Ordering},
        RwLock,
    },
};

use ipfd_impl::Breakpoint;
use ipfd_thread::{IpfdMessage, IpfdThread};
use windows::{
    core::{Owned, HRESULT},
    Win32::{
        Foundation::*,
        System::{
            SystemServices::*,
            Threading::{CreateEventW, GetCurrentThreadId, WaitForSingleObject, INFINITE},
        },
    },
};

static THREAD: RwLock<Option<IpfdThread>> = RwLock::new(None);
static RC: AtomicUsize = AtomicUsize::new(0);

#[no_mangle]
#[allow(non_snake_case, unused_variables)]
pub extern "system" fn DllMain(dll_module: HINSTANCE, call_reason: u32, _: *mut ()) -> bool {
    match call_reason {
        DLL_THREAD_ATTACH => {
            let _ = send(IpfdMessage::ThreadRefreshBreakpoints {
                thread_id: unsafe { GetCurrentThreadId() },
            });
        }
        _ => (),
    }

    true
}

fn send(message: IpfdMessage) -> HRESULT {
    let guard = match THREAD.read() {
        Ok(guard) => guard,
        Err(_) => {
            return ERROR_INVALID_STATE.to_hresult();
        }
    };
    if let Some(thread) = guard.as_ref() {
        thread.send(message);
        S_OK
    } else {
        ERROR_INVALID_STATE.to_hresult()
    }
}

#[no_mangle]
pub extern "C" fn ipfd_initialize() -> HRESULT {
    match RC.fetch_add(1, Ordering::SeqCst) {
        0 => {
            let mut guard = match THREAD.write() {
                Ok(guard) => guard,
                Err(_) => {
                    return ERROR_INVALID_STATE.to_hresult();
                }
            };
            forget(guard.replace(IpfdThread::new()));
        }
        usize::MAX => panic!(),
        _ => {}
    }
    S_OK
}

#[no_mangle]
pub extern "C" fn ipfd_terminate() -> HRESULT {
    match RC.fetch_sub(1, Ordering::SeqCst) {
        0 => panic!(),
        1 => {
            let mut guard = match THREAD.write() {
                Ok(guard) => guard,
                Err(_) => {
                    return ERROR_INVALID_STATE.to_hresult();
                }
            };
            drop(guard.take());
        }
        _ => {}
    }
    S_OK
}

#[no_mangle]
pub extern "C" fn ipfd_set_breakpoint(index: u8, address: usize, flags: u8) -> HRESULT {
    if index > 3 || (flags & !0xF3u8) != 0 {
        return E_INVALIDARG;
    }

    send(IpfdMessage::SetBreakpoint {
        index,
        breakpoint: Breakpoint { flags, address },
    })
}

#[no_mangle]
pub extern "C" fn ipfd_refresh_all_breakpoints() -> HRESULT {
    send(IpfdMessage::RefreshAllBreakpoints)
}

#[no_mangle]
pub extern "C" fn ipfd_clear_all_breakpoints() -> HRESULT {
    send(IpfdMessage::ClearAllBreakpoints)
}

#[no_mangle]
pub extern "C" fn ipfd_memmove(source: usize, destination: usize, size: usize) -> HRESULT {
    send(IpfdMessage::MemoryCopy {
        source,
        destination,
        size,
    })
}

#[no_mangle]
pub extern "C" fn ipfd_set_event(hevent: usize) -> HRESULT {
    send(IpfdMessage::SetEvent { hevent })
}

#[no_mangle]
pub extern "C" fn ipfd_sync() -> HRESULT {
    let hevent: Owned<HANDLE> = unsafe {
        match CreateEventW(None, true, false, None) {
            Ok(hevent) => Owned::new(hevent),
            Err(err) => {
                return err.code();
            }
        }
    };
    let hr = send(IpfdMessage::SetEvent {
        hevent: hevent.0 as usize,
    });
    if hr.is_err() {
        return hr;
    }
    if unsafe { WaitForSingleObject(*hevent, INFINITE) } == WAIT_OBJECT_0 {
        S_OK
    } else {
        E_FAIL
    }
}

#[no_mangle]
pub extern "C" fn ipfd_set_breakpoint_callback(callback: usize) -> HRESULT {
    ipfd_impl::BREAKPOINT_CALLBACK.store(callback, Ordering::SeqCst);
    HRESULT(0)
}
