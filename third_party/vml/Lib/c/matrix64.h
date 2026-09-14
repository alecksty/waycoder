/* matrix64.h - Double-Precision Matrix & Vector Math */

#ifndef _MATRIX64_H
#define _MATRIX64_H

#param lib("matrix64")

/* 2×2 矩阵 */
void mat2_identity_d(double* result);
void mat2_add_d(double* a, double* b, double* result);
void mat2_sub_d(double* a, double* b, double* result);
void mat2_mul_d(double* a, double* b, double* result);
void mat2_scale_d(double* a, double scalar, double* result);
double mat2_det_d(double* a);
void mat2_transpose_d(double* a, double* result);
int mat2_inv_d(double* a, double* result);
void mat2_rot_d(double angle_rad, double* result);

/* 3×3 矩阵 */
void mat3_identity_d(double* result);
void mat3_mul_d(double* a, double* b, double* result);
double mat3_det_d(double* a);
void mat3_transpose_d(double* a, double* result);
void mat3_scale_d(double* a, double scalar, double* result);

/* 4×4 矩阵 */
void mat4_identity_d(double* result);
void mat4_mul_d(double* a, double* b, double* result);
void mat4_translate_d(double tx, double ty, double tz, double* result);
void mat4_scale_xyz_d(double sx, double sy, double sz, double* result);
void mat4_rot_x_d(double angle_rad, double* result);
void mat4_rot_y_d(double angle_rad, double* result);
void mat4_rot_z_d(double angle_rad, double* result);
void mat4_transpose_d(double* a, double* result);

/* 2D 向量 */
double vec2_dot_d(double* a, double* b);
double vec2_cross_d(double* a, double* b);
double vec2_len_d(double* v);
double vec2_normalize_d(double* v);

/* 3D 向量 */
double vec3_dot_d(double* a, double* b);
void vec3_cross_d(double* a, double* b, double* result);
double vec3_len_d(double* v);
double vec3_normalize_d(double* v);
void mat4_transform_vec3_d(double* m, double* v, double* result);

#endif /* _MATRIX64_H */
