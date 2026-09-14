#param lib("math64")

// VML Shared Matrix64 Library — Double-Precision Matrix & Vector Math
// All functions use double and double* arrays (64-bit float)

// ===== 2×2 矩阵 =====

__stdcall void mat2_identity_d(double* result) {
    result[0] = 1.0; result[1] = 0.0;
    result[2] = 0.0; result[3] = 1.0;
}

__stdcall void mat2_add_d(double* a, double* b, double* result) {
    result[0] = a[0] + b[0]; result[1] = a[1] + b[1];
    result[2] = a[2] + b[2]; result[3] = a[3] + b[3];
}

__stdcall void mat2_sub_d(double* a, double* b, double* result) {
    result[0] = a[0] - b[0]; result[1] = a[1] - b[1];
    result[2] = a[2] - b[2]; result[3] = a[3] - b[3];
}

__stdcall void mat2_mul_d(double* a, double* b, double* result) {
    double r0 = a[0]*b[0] + a[1]*b[2];
    double r1 = a[0]*b[1] + a[1]*b[3];
    double r2 = a[2]*b[0] + a[3]*b[2];
    double r3 = a[2]*b[1] + a[3]*b[3];
    result[0] = r0; result[1] = r1; result[2] = r2; result[3] = r3;
}

__stdcall void mat2_scale_d(double* a, double scalar, double* result) {
    result[0] = a[0]*scalar; result[1] = a[1]*scalar;
    result[2] = a[2]*scalar; result[3] = a[3]*scalar;
}

__stdcall double mat2_det_d(double* a) {
    return a[0]*a[3] - a[1]*a[2];
}

__stdcall void mat2_transpose_d(double* a, double* result) {
    result[0] = a[0]; result[1] = a[2];
    result[2] = a[1]; result[3] = a[3];
}

__stdcall int mat2_inv_d(double* a, double* result) {
    double det = a[0]*a[3] - a[1]*a[2];
    if (det == 0.0) return 0;
    double inv = 1.0 / det;
    result[0] = a[3]*inv; result[1] = -a[1]*inv;
    result[2] = -a[2]*inv; result[3] = a[0]*inv;
    return 1;
}

__stdcall void mat2_rot_d(double angle_rad, double* result) {
    double c = cos_deg_d(angle_rad * 180.0 / 3.141592653589793);
    double s = sin_deg_d(angle_rad * 180.0 / 3.141592653589793);
    result[0] = c; result[1] = -s;
    result[2] = s; result[3] = c;
}

// ===== 3×3 矩阵 =====

__stdcall void mat3_identity_d(double* result) {
    result[0]=1;result[1]=0;result[2]=0;
    result[3]=0;result[4]=1;result[5]=0;
    result[6]=0;result[7]=0;result[8]=1;
}

__stdcall void mat3_mul_d(double* a, double* b, double* result) {
    double t[9]; int i, j, k;
    for(i=0;i<3;i++) for(j=0;j<3;j++) {
        t[i*3+j]=0;
        for(k=0;k<3;k++) t[i*3+j] += a[i*3+k]*b[k*3+j];
    }
    for(i=0;i<9;i++) result[i]=t[i];
}

__stdcall double mat3_det_d(double* a) {
    return a[0]*(a[4]*a[8]-a[5]*a[7])
          -a[1]*(a[3]*a[8]-a[5]*a[6])
          +a[2]*(a[3]*a[7]-a[4]*a[6]);
}

__stdcall void mat3_transpose_d(double* a, double* result) {
    result[0]=a[0];result[1]=a[3];result[2]=a[6];
    result[3]=a[1];result[4]=a[4];result[5]=a[7];
    result[6]=a[2];result[7]=a[5];result[8]=a[8];
}

__stdcall void mat3_scale_d(double* a, double scalar, double* result) {
    int i; for(i=0;i<9;i++) result[i]=a[i]*scalar;
}

// ===== 4×4 矩阵 =====

__stdcall void mat4_identity_d(double* result) {
    int i; for(i=0;i<16;i++) result[i]=(i%5==0)?1.0:0.0;
}

__stdcall void mat4_mul_d(double* a, double* b, double* result) {
    double t[16]; int i, j, k;
    for(i=0;i<4;i++) for(j=0;j<4;j++) {
        t[i*4+j]=0;
        for(k=0;k<4;k++) t[i*4+j] += a[i*4+k]*b[k*4+j];
    }
    for(i=0;i<16;i++) result[i]=t[i];
}

__stdcall void mat4_translate_d(double tx, double ty, double tz, double* result) {
    mat4_identity_d(result);
    result[3]=tx; result[7]=ty; result[11]=tz;
}

__stdcall void mat4_scale_xyz_d(double sx, double sy, double sz, double* result) {
    mat4_identity_d(result);
    result[0]=sx; result[5]=sy; result[10]=sz;
}

__stdcall void mat4_rot_x_d(double angle_rad, double* result) {
    double c = cos_deg_d(angle_rad * 180.0 / 3.141592653589793);
    double s = sin_deg_d(angle_rad * 180.0 / 3.141592653589793);
    mat4_identity_d(result);
    result[5]=c; result[6]=-s; result[9]=s; result[10]=c;
}

__stdcall void mat4_rot_y_d(double angle_rad, double* result) {
    double c = cos_deg_d(angle_rad * 180.0 / 3.141592653589793);
    double s = sin_deg_d(angle_rad * 180.0 / 3.141592653589793);
    mat4_identity_d(result);
    result[0]=c; result[2]=s; result[8]=-s; result[10]=c;
}

__stdcall void mat4_rot_z_d(double angle_rad, double* result) {
    double c = cos_deg_d(angle_rad * 180.0 / 3.141592653589793);
    double s = sin_deg_d(angle_rad * 180.0 / 3.141592653589793);
    mat4_identity_d(result);
    result[0]=c; result[1]=-s; result[4]=s; result[5]=c;
}

__stdcall void mat4_transpose_d(double* a, double* result) {
    int i,j; for(i=0;i<4;i++) for(j=0;j<4;j++) result[j*4+i]=a[i*4+j];
}

// ===== 2D 向量 (double) =====

__stdcall double vec2_dot_d(double* a, double* b) { return a[0]*b[0] + a[1]*b[1]; }
__stdcall double vec2_cross_d(double* a, double* b) { return a[0]*b[1] - a[1]*b[0]; }

__stdcall double vec2_len_d(double* v) {
    double sq = v[0]*v[0] + v[1]*v[1];
    if (sq <= 0.0) return 0.0;
    double g = sq*0.5 + 0.5; int i;
    for(i=0;i<15;i++) { double ng=(g+sq/g)*0.5; if(ng>=g) return g; g=ng; }
    return g;
}

__stdcall double vec2_normalize_d(double* v) {
    double len = vec2_len_d(v);
    if (len > 0.0) { v[0]/=len; v[1]/=len; }
    return len;
}

// ===== 3D 向量 (double) =====

__stdcall double vec3_dot_d(double* a, double* b) { return a[0]*b[0]+a[1]*b[1]+a[2]*b[2]; }

__stdcall void vec3_cross_d(double* a, double* b, double* result) {
    result[0]=a[1]*b[2]-a[2]*b[1];
    result[1]=a[2]*b[0]-a[0]*b[2];
    result[2]=a[0]*b[1]-a[1]*b[0];
}

__stdcall double vec3_len_d(double* v) {
    double sq = v[0]*v[0]+v[1]*v[1]+v[2]*v[2];
    if (sq<=0.0) return 0.0;
    double g=sq*0.5+0.5; int i;
    for(i=0;i<15;i++){double ng=(g+sq/g)*0.5; if(ng>=g) return g; g=ng;}
    return g;
}

__stdcall double vec3_normalize_d(double* v) {
    double len = vec3_len_d(v);
    if (len>0.0) { v[0]/=len; v[1]/=len; v[2]/=len; }
    return len;
}

__stdcall void mat4_transform_vec3_d(double* m, double* v, double* result) {
    result[0]=m[0]*v[0]+m[1]*v[1]+m[2]*v[2]+m[3];
    result[1]=m[4]*v[0]+m[5]*v[1]+m[6]*v[2]+m[7];
    result[2]=m[8]*v[0]+m[9]*v[1]+m[10]*v[2]+m[11];
}
