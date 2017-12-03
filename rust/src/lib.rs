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

pub struct Vector3 {
    x: f32,
    y: f32,
    z: f32
}

impl Vector3 {
    #[no_mangle]
    pub extern fn magnitude(&self) -> f32 {
        (self.x * self.x + self.y * self.y + self.z * self.z).sqrt()
    }
}

#[no_mangle]
pub extern fn maybe_pos() -> Option<Vector3> {
    let vector = Vector3{
        x: 1.0,
        y: 2.0,
        z: 3.0
    };
    Some(vector)
}
