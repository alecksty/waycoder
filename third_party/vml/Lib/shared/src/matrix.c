// VML Shared Matrix Library
// 矩阵运算库 — MCU兼容，无动态内存分配
// 矩阵以行优先 float 数组存储: 2x2=[m00,m01,m10,m11], 3x3=[m00,m01,m02,m10,...]

// ===== 2x2 矩阵 =====

// mat2_identity: 单位矩阵
__stdcall void mat2_identity(float* result) {
    result[0] = 1.0f; result[1] = 0.0f;
    result[2] = 0.0f; result[3] = 1.0f;
}

// mat2_add: result = a + b
__stdcall void mat2_add(float* a, float* b, float* result) {
    result[0] = a[0] + b[0]; result[1] = a[1] + b[1];
    result[2] = a[2] + b[2]; result[3] = a[3] + b[3];
}

// mat2_sub: result = a - b
__stdcall void mat2_sub(float* a, float* b, float* result) {
    result[0] = a[0] - b[0]; result[1] = a[1] - b[1];
    result[2] = a[2] - b[2]; result[3] = a[3] - b[3];
}

// mat2_mul: result = a * b
__stdcall void mat2_mul(float* a, float* b, float* result) {
    result[0] = a[0]*b[0] + a[1]*b[2];
    result[1] = a[0]*b[1] + a[1]*b[3];
    result[2] = a[2]*b[0] + a[3]*b[2];
    result[3] = a[2]*b[1] + a[3]*b[3];
}

// mat2_scale: result = a * scalar
__stdcall void mat2_scale(float* a, float scalar, float* result) {
    result[0] = a[0] * scalar; result[1] = a[1] * scalar;
    result[2] = a[2] * scalar; result[3] = a[3] * scalar;
}

// mat2_det: determinant
__stdcall float mat2_det(float* a) {
    return a[0]*a[3] - a[1]*a[2];
}

// mat2_transpose: result = a^T
__stdcall void mat2_transpose(float* a, float* result) {
    result[0] = a[0]; result[1] = a[2];
    result[2] = a[1]; result[3] = a[3];
}

// mat2_inv: result = a^(-1), returns 1 if invertible, 0 if singular
__stdcall int mat2_inv(float* a, float* result) {
    float det = a[0]*a[3] - a[1]*a[2];
    if (det < 1e-10f && det > -1e-10f) return 0;
    float inv_det = 1.0f / det;
    result[0] =  a[3] * inv_det;
    result[1] = -a[1] * inv_det;
    result[2] = -a[2] * inv_det;
    result[3] =  a[0] * inv_det;
    return 1;
}

// mat2_rot: 2D rotation matrix [cos, -sin; sin, cos]
__stdcall void mat2_rot(float angle_rad, float* result) {
    // Simple sin/cos via Taylor series
    float a2 = angle_rad * angle_rad;
    float sin_a = angle_rad - angle_rad*a2*0.166667f + angle_rad*a2*a2*0.00833333f;
    float cos_a = 1.0f - a2*0.5f + a2*a2*0.0416667f - a2*a2*a2*0.00138889f;
    result[0] = cos_a; result[1] = -sin_a;
    result[2] = sin_a; result[3] =  cos_a;
}

// ===== 3x3 矩阵 =====

// mat3_identity: 3x3 单位矩阵
__stdcall void mat3_identity(float* result) {
    result[0] = 1.0f; result[1] = 0.0f; result[2] = 0.0f;
    result[3] = 0.0f; result[4] = 1.0f; result[5] = 0.0f;
    result[6] = 0.0f; result[7] = 0.0f; result[8] = 1.0f;
}

// mat3_mul: result = a * b
__stdcall void mat3_mul(float* a, float* b, float* result) {
    result[0] = a[0]*b[0] + a[1]*b[3] + a[2]*b[6];
    result[1] = a[0]*b[1] + a[1]*b[4] + a[2]*b[7];
    result[2] = a[0]*b[2] + a[1]*b[5] + a[2]*b[8];
    result[3] = a[3]*b[0] + a[4]*b[3] + a[5]*b[6];
    result[4] = a[3]*b[1] + a[4]*b[4] + a[5]*b[7];
    result[5] = a[3]*b[2] + a[4]*b[5] + a[5]*b[8];
    result[6] = a[6]*b[0] + a[7]*b[3] + a[8]*b[6];
    result[7] = a[6]*b[1] + a[7]*b[4] + a[8]*b[7];
    result[8] = a[6]*b[2] + a[7]*b[5] + a[8]*b[8];
}

// mat3_det: 3x3 determinant
__stdcall float mat3_det(float* a) {
    return a[0]*(a[4]*a[8] - a[5]*a[7])
         - a[1]*(a[3]*a[8] - a[5]*a[6])
         + a[2]*(a[3]*a[7] - a[4]*a[6]);
}

// mat3_transpose: 3x3 transpose
__stdcall void mat3_transpose(float* a, float* result) {
    result[0] = a[0]; result[1] = a[3]; result[2] = a[6];
    result[3] = a[1]; result[4] = a[4]; result[5] = a[7];
    result[6] = a[2]; result[7] = a[5]; result[8] = a[8];
}

// mat3_scale: result = a * scalar
__stdcall void mat3_scale(float* a, float scalar, float* result) {
    result[0] = a[0]*scalar; result[1] = a[1]*scalar; result[2] = a[2]*scalar;
    result[3] = a[3]*scalar; result[4] = a[4]*scalar; result[5] = a[5]*scalar;
    result[6] = a[6]*scalar; result[7] = a[7]*scalar; result[8] = a[8]*scalar;
}

// ===== 4x4 矩阵 (3D graphics) =====

// mat4_identity: 4x4 单位矩阵
__stdcall void mat4_identity(float* result) {
    int i;
    for (i = 0; i < 16; i++) result[i] = 0.0f;
    result[0] = 1.0f; result[5] = 1.0f; result[10] = 1.0f; result[15] = 1.0f;
}

// mat4_mul: 4x4 matrix multiplication
__stdcall void mat4_mul(float* a, float* b, float* result) {
    int row, col;
    for (row = 0; row < 4; row++) {
        for (col = 0; col < 4; col++) {
            float sum = 0.0f;
            int k;
            for (k = 0; k < 4; k++)
                sum += a[row*4 + k] * b[k*4 + col];
            result[row*4 + col] = sum;
        }
    }
}

// mat4_translate: 4x4 translation matrix
__stdcall void mat4_translate(float tx, float ty, float tz, float* result) {
    mat4_identity(result);
    result[12] = tx; result[13] = ty; result[14] = tz;
}

// mat4_scale_xyz: 4x4 scale matrix
__stdcall void mat4_scale_xyz(float sx, float sy, float sz, float* result) {
    int i;
    for (i = 0; i < 16; i++) result[i] = 0.0f;
    result[0] = sx; result[5] = sy; result[10] = sz; result[15] = 1.0f;
}

// mat4_rot_x: 4x4 rotation around X axis
__stdcall void mat4_rot_x(float angle_rad, float* result) {
    float a2 = angle_rad * angle_rad;
    float s = angle_rad - angle_rad*a2*0.166667f + angle_rad*a2*a2*0.00833333f;
    float c = 1.0f - a2*0.5f + a2*a2*0.0416667f - a2*a2*a2*0.00138889f;
    int i;
    for (i = 0; i < 16; i++) result[i] = 0.0f;
    result[0] = 1.0f;
    result[5] = c;  result[6] = -s;
    result[9] = s;  result[10] = c;
    result[15] = 1.0f;
}

// mat4_rot_y: 4x4 rotation around Y axis
__stdcall void mat4_rot_y(float angle_rad, float* result) {
    float a2 = angle_rad * angle_rad;
    float s = angle_rad - angle_rad*a2*0.166667f + angle_rad*a2*a2*0.00833333f;
    float c = 1.0f - a2*0.5f + a2*a2*0.0416667f - a2*a2*a2*0.00138889f;
    int i;
    for (i = 0; i < 16; i++) result[i] = 0.0f;
    result[0] = c;   result[2] = s;
    result[5] = 1.0f;
    result[8] = -s;  result[10] = c;
    result[15] = 1.0f;
}

// mat4_rot_z: 4x4 rotation around Z axis
__stdcall void mat4_rot_z(float angle_rad, float* result) {
    float a2 = angle_rad * angle_rad;
    float s = angle_rad - angle_rad*a2*0.166667f + angle_rad*a2*a2*0.00833333f;
    float c = 1.0f - a2*0.5f + a2*a2*0.0416667f - a2*a2*a2*0.00138889f;
    int i;
    for (i = 0; i < 16; i++) result[i] = 0.0f;
    result[0] = c;  result[1] = -s;
    result[4] = s;  result[5] = c;
    result[10] = 1.0f;
    result[15] = 1.0f;
}

// mat4_transpose: 4x4 transpose
__stdcall void mat4_transpose(float* a, float* result) {
    result[0] = a[0];  result[1] = a[4];  result[2] = a[8];   result[3] = a[12];
    result[4] = a[1];  result[5] = a[5];  result[6] = a[9];   result[7] = a[13];
    result[8] = a[2];  result[9] = a[6];  result[10] = a[10]; result[11] = a[14];
    result[12]= a[3];  result[13]= a[7];  result[14]= a[11];  result[15]= a[15];
}

// ===== 向量运算 =====

// vec2_dot: 2D dot product
__stdcall float vec2_dot(float* a, float* b) {
    return a[0]*b[0] + a[1]*b[1];
}

// vec2_cross: 2D cross product (scalar)
__stdcall float vec2_cross(float* a, float* b) {
    return a[0]*b[1] - a[1]*b[0];
}

// vec2_len: 2D vector length
__stdcall float vec2_len(float* v) {
    float s = v[0]*v[0] + v[1]*v[1];
    if (s <= 0.0f) return 0.0f;
    float g = s * 0.5f + 0.5f;
    int i;
    for (i = 0; i < 10; i++) g = (g + s/g) * 0.5f;
    return g;
}

// vec2_normalize: normalize 2D vector in-place, returns original length
__stdcall float vec2_normalize(float* v) {
    float len = vec2_len(v);
    if (len > 1e-10f) { v[0] /= len; v[1] /= len; }
    return len;
}

// vec3_dot: 3D dot product
__stdcall float vec3_dot(float* a, float* b) {
    return a[0]*b[0] + a[1]*b[1] + a[2]*b[2];
}

// vec3_cross: 3D cross product, result = a x b
__stdcall void vec3_cross(float* a, float* b, float* result) {
    result[0] = a[1]*b[2] - a[2]*b[1];
    result[1] = a[2]*b[0] - a[0]*b[2];
    result[2] = a[0]*b[1] - a[1]*b[0];
}

// vec3_len: 3D vector length
__stdcall float vec3_len(float* v) {
    float s = v[0]*v[0] + v[1]*v[1] + v[2]*v[2];
    if (s <= 0.0f) return 0.0f;
    float g = s * 0.5f + 0.5f;
    int i;
    for (i = 0; i < 10; i++) g = (g + s/g) * 0.5f;
    return g;
}

// vec3_normalize: normalize 3D vector in-place, returns original length
__stdcall float vec3_normalize(float* v) {
    float len = vec3_len(v);
    if (len > 1e-10f) { v[0] /= len; v[1] /= len; v[2] /= len; }
    return len;
}

// mat4_transform_vec3: 4x4 matrix * 3D vector (w=1, perspective divide)
__stdcall void mat4_transform_vec3(float* m, float* v, float* result) {
    float x = m[0]*v[0] + m[1]*v[1] + m[2]*v[2]  + m[3];
    float y = m[4]*v[0] + m[5]*v[1] + m[6]*v[2]  + m[7];
    float z = m[8]*v[0] + m[9]*v[1] + m[10]*v[2] + m[11];
    float w = m[12]*v[0]+ m[13]*v[1]+ m[14]*v[2] + m[15];
    if (w < 1e-10f && w > -1e-10f) w = 1.0f;
    result[0] = x / w;
    result[1] = y / w;
    result[2] = z / w;
}
