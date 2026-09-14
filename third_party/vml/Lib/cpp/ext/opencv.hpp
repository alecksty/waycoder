// OpenCV FFI Bindings — C++
#ifndef VML_EXT_OPENCV_HPP
#define VML_EXT_OPENCV_HPP

namespace vml::ext::opencv {

inline int  imread(const char* p)       { return 0; }
inline int  imwrite(const char* p, int h){ return 0; }
inline int  imshow(const char* t, int h) { return 0; }
inline int  waitkey(int d)              { return 0; }
inline void cvtcolor(int s, int d, int c){}
inline void resize(int s, int d, int w, int h){}
inline void rectangle(int i, int x1, int y1, int x2, int y2, int r, int g, int b, int t){}
inline void circle(int i, int cx, int cy, int rad, int r, int g, int b, int t){}
inline void line(int i, int x1, int y1, int x2, int y2, int r, int g, int b, int t){}
inline void puttext(int i, const char* t, int x, int y, int r, int g, int b){}
inline int  width(int h)               { return 0; }
inline int  height(int h)              { return 0; }
inline int  channels(int h)            { return 0; }
inline void blur(int s, int d, int k)   {}
inline void canny(int s, int d, int l, int hi){}
inline void threshold(int s, int d, int th, int mv, int ty){}
inline int  facedetect(int i, const char* c){ return 0; }
inline void release(int h)              {}

} // namespace vml::ext::opencv
#endif
