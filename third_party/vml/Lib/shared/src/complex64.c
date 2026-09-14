#param lib("math")

// VML Shared Complex64 Library — Double-Precision Complex Numbers
// All functions use double[2] arrays for (real, imag)

__stdcall void zadd(double* a, double* b, double* result) {
    result[0] = a[0] + b[0]; result[1] = a[1] + b[1];
}

__stdcall void zsub(double* a, double* b, double* result) {
    result[0] = a[0] - b[0]; result[1] = a[1] - b[1];
}

__stdcall void zmul(double* a, double* b, double* result) {
    result[0] = a[0]*b[0] - a[1]*b[1];
    result[1] = a[0]*b[1] + a[1]*b[0];
}

__stdcall void zdiv(double* a, double* b, double* result) {
    double denom = b[0]*b[0] + b[1]*b[1];
    if (denom == 0.0) { result[0]=0.0; result[1]=0.0; return; }
    result[0] = (a[0]*b[0] + a[1]*b[1]) / denom;
    result[1] = (a[1]*b[0] - a[0]*b[1]) / denom;
}

__stdcall double zabs(double* z) {
    double sq = z[0]*z[0] + z[1]*z[1];
    if (sq <= 0.0) return 0.0;
    double g = sq*0.5 + 0.5; int i;
    for(i=0;i<15;i++) { double ng=(g+sq/g)*0.5; if(ng>=g) return g; g=ng; }
    return g;
}

__stdcall double zarg(double* z) {
    if (z[0] == 0.0 && z[1] == 0.0) return 0.0;
    // atan2 via arctan approximation
    double y = z[1], x = z[0];
    double abs_y = y < 0 ? -y : y;
    double abs_x = x < 0 ? -x : x;
    double r;
    if (abs_x > abs_y) {
        r = abs_y / abs_x;
        double t = r, sum = r; int n;
        for (n=1; n<15; n++) { t*=-r*r*(2*n-1)/(2*n+1); sum+=t; }
        r = sum;
        if (x < 0) r = 3.141592653589793 - r;
        if (y < 0) r = -r;
    } else {
        if (abs_y == 0) return 0.0;
        r = abs_x / abs_y;
        double t = r, sum = r; int n;
        for (n=1; n<15; n++) { t*=-r*r*(2*n-1)/(2*n+1); sum+=t; }
        r = 1.570796326794896 - sum;
        if (y < 0) r = -r;
        if (x < 0) r = 3.141592653589793 - (y > 0 ? r : -r);
    }
    return r;
}

__stdcall void zconj(double* a, double* result) {
    result[0] = a[0]; result[1] = -a[1];
}

__stdcall void zneg(double* a, double* result) {
    result[0] = -a[0]; result[1] = -a[1];
}

__stdcall double zexp_re(double re, double im) {
    // exp(re) * cos(im)
    double e = 1.0, term = 1.0;
    int i;
    double x = re;
    if (x < 0) x = -x;
    for (i=1; i<20; i++) { term*=x/i; e+=term; }
    if (re < 0) e = 1.0/e;
    return e * (im < 1e-15 && im > -1e-15 ? 1.0 : 0.0);
}

__stdcall double zexp_im(double re, double im) {
    double e = 1.0, term = 1.0;
    int i;
    double x = re;
    if (x < 0) x = -x;
    for (i=1; i<20; i++) { term*=x/i; e+=term; }
    if (re < 0) e = 1.0/e;
    // sin(im) via Taylor
    double s = im, st = im;
    for (i=1; i<12; i++) { st*=-im*im/((2*i)*(2*i+1)); s+=st; }
    return e * s;
}

__stdcall void zsqr(double* a, double* result) {
    result[0] = a[0]*a[0] - a[1]*a[1];
    result[1] = 2.0 * a[0] * a[1];
}

__stdcall void zsqrt(double* z, double* result) {
    double r = zabs(z);
    if (r == 0.0) { result[0]=0.0; result[1]=0.0; return; }
    double re = (z[0] + r) * 0.5;
    double im = (r - z[0]) * 0.5;
    double g = re > 0 ? 1.0 : 0.0;
    double h;
    if (re <= 0) { re = 0; g = 0; }
    g = re*0.5+0.5; for (h=0;h<15;h++) { double ng=(g+re/g)*0.5; if(ng>=g) break; g=ng; }
    result[0] = g;
    if (z[1] < 0) result[0] = -result[0];
    if (im <= 0) { result[1] = 0; return; }
    g = im*0.5+0.5; for (h=0;h<15;h++) { double ng=(g+im/g)*0.5; if(ng>=g) break; g=ng; }
    result[1] = g;
}
