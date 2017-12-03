extern crate libc;

use libc::{c_char, uint32_t};
use std::ffi::CStr;

#[no_mangle]
pub extern fn maybe_parse(s: *const c_char) -> Option<i32> {
    let c_str = unsafe {
        assert!(!s.is_null());

        CStr::from_ptr(s)
    };

    let text = c_str.to_str().unwrap();
    text.parse().ok()
}
