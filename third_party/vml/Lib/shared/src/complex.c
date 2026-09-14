#param lib("fixed")
#param lib("math")

// VML Shared Complex Number Library
// 复数运算库 — MCU兼容，无动态内存分配
// 复数表示为连续两个 float: [real, imag]

// cadd: result = a + b
__stdcall void cadd(float* a, float* b, float* result) {
    result[0] = a[0] + b[0];
    result[1] = a[1] + b[1];
}

// csub: result = a - b
__stdcall void csub(float* a, float* b, float* result) {
    result[0] = a[0] - b[0];
    result[1] = a[1] - b[1];
}

// cmul: result = a * b  (complex multiplication)
__stdcall void cmul(float* a, float* b, float* result) {
    // (a.re + i*a.im) * (b.re + i*b.im)
    // = (a.re*b.re - a.im*b.im) + i*(a.re*b.im + a.im*b.re)
    result[0] = a[0]*b[0] - a[1]*b[1];
    result[1] = a[0]*b[1] + a[1]*b[0];
}

// cdiv: result = a / b
__stdcall void cdiv(float* a, float* b, float* result) {
    // (a.re + i*a.im) / (b.re + i*b.im)
    float denom = b[0]*b[0] + b[1]*b[1];
    if (denom < 1e-10f && denom > -1e-10f) {
        result[0] = 0.0f;
        result[1] = 0.0f;
        return;
    }
    result[0] = (a[0]*b[0] + a[1]*b[1]) / denom;
    result[1] = (a[1]*b[0] - a[0]*b[1]) / denom;
}

// complex_abs: magnitude |z| = sqrt(re^2 + im^2)
__stdcall float complex_abs(float* z) {
    float sum = z[0]*z[0] + z[1]*z[1];
    // Newton's method for sqrt
    if (sum <= 0.0f) return 0.0f;
    float guess = sum * 0.5f + 0.5f;
    float prev;
    int i;
    for (i = 0; i < 10; i++) {
        prev = guess;
        guess = (guess + sum / guess) * 0.5f;
        if (guess >= prev - 1e-6f && guess <= prev + 1e-6f) break;
    }
    return guess;
}

// complex_arg: argument/phase angle in radians (-PI to PI)
__stdcall float complex_arg(float* z) {
    // atan2(im, re)
    float x = z[0];
    float y = z[1];
    // Simple atan2 approximation using piecewise
    if (x > 0.0f) {
        float t = y / x;
        return t / (1.0f + 0.28f * t * t);  // fast atan approx
    }
    if (x < 0.0f) {
        float t = y / x;
        float at = t / (1.0f + 0.28f * t * t);
        if (y >= 0.0f) return at + 3.1415927f;
        return at - 3.1415927f;
    }
    if (y > 0.0f) return 1.5707963f;
    if (y < 0.0f) return -1.5707963f;
    return 0.0f;
}

// cconj: complex conjugate, result = conj(a)
__stdcall void cconj(float* a, float* result) {
    result[0] = a[0];
    result[1] = -a[1];
}

// cneg: negation, result = -a
__stdcall void cneg(float* a, float* result) {
    result[0] = -a[0];
    result[1] = -a[1];
}

// cexp: e^z = e^re * (cos(im) + i*sin(im))
// Uses Taylor series for exp and sin/cos from math.vml
__stdcall float cexp_re(float re, float im) {
    // real part: e^re * cos(im)
    // Use Taylor: e^re * (1 - im^2/2! + im^4/4! - ...)
    float im2 = im * im;
    float cos_im = 1.0f - im2 * 0.5f + im2 * im2 * 0.0416667f - im2 * im2 * im2 * 0.00138889f;
    // Simple exp: 1 + re + re^2/2! + re^3/3!
    float exp_re = 1.0f + re + re*re*0.5f + re*re*re*0.166667f + re*re*re*re*0.0416667f;
    return exp_re * cos_im;
}

__stdcall float cexp_im(float re, float im) {
    // imag part: e^re * sin(im)
    float im2 = im * im;
    float sin_im = im - im*im2*0.166667f + im*im2*im2*0.00833333f;
    float exp_re = 1.0f + re + re*re*0.5f + re*re*re*0.166667f + re*re*re*re*0.0416667f;
    return exp_re * sin_im;
}

// csqr: square of a complex number z^2 = (re^2 - im^2) + i*(2*re*im)
__stdcall void csqr(float* a, float* result) {
    result[0] = a[0]*a[0] - a[1]*a[1];
    result[1] = 2.0f * a[0] * a[1];
}

// csqrt: principal square root of complex number
__stdcall void csqrt(float* z, float* result) {
    float r = complex_abs(z);
    if (r < 1e-10f) {
        result[0] = 0.0f;
        result[1] = 0.0f;
        return;
    }
    float re = z[0];
    float im = z[1];
    if (re >= 0.0f) {
        float s = (r + re) * 0.5f;
        if (s > 0.0f) {
            float g = s * 0.5f + 0.5f;
            int i;
            for (i = 0; i < 8; i++) g = (g + s/g) * 0.5f;
            result[0] = g;
        } else {
            result[0] = 0.0f;
        }
        result[1] = result[0] > 1e-10f ? im / (2.0f * result[0]) : 0.0f;
    } else {
        float s = (r - re) * 0.5f;
        if (s > 0.0f) {
            float g = s * 0.5f + 0.5f;
            int i;
            for (i = 0; i < 8; i++) g = (g + s/g) * 0.5f;
            result[1] = (im >= 0.0f) ? g : -g;
        } else {
            result[1] = 0.0f;
        }
        result[0] = result[1] > 1e-10f ? im / (2.0f * result[1]) : 0.0f;
    }
}
