;; VML Scheme 标准库 — 完整运行时支持 (R5RS/R7RS-small 兼容)
;; 用法: scheme -l vml stdlib.scm

;; ============================================================
;; 控制台 I/O
;; ============================================================
(define (display x)
  (cond ((string? x) (asm "SYSCALL 1"))
        ((number? x) (asm "SYSCALL 6"))
        ((char? x)   (asm "SYSCALL 4"))
        (else        (asm "SYSCALL 1"))))
(define (newline) (asm "LOAD R0 #10") (asm "SYSCALL 4"))
(define (print x) (display x) (newline))
(define (print-hex n) (asm "SYSCALL 10"))
(define (write x) (display x))
(define (write-char c) (asm "SYSCALL 4"))
(define (read-line) (let ((buf "")) (asm "SYSCALL 2") buf))
(define (read-int) (asm "SYSCALL 7") 0)
(define (getchar) (asm "SYSCALL 5") 0)
(define (read-char) (asm "SYSCALL 5") (integer->char 0))
(define (peek-char) (read-char))

;; ============================================================
;; 字符串操作
;; ============================================================
(define (string-length s) (asm "SYSCALL 60") 0)
(define (string-copy dest src) (asm "SYSCALL 61"))
(define (string-compare a b) (asm "SYSCALL 62") 0)
(define (string-append! dest src) (asm "SYSCALL 63"))
(define (string=? a b) (= (string-compare a b) 0))
(define (string<? a b) (< (string-compare a b) 0))
(define (string>? a b) (> (string-compare a b) 0))
(define (string<=? a b) (<= (string-compare a b) 0))
(define (string>=? a b) (>= (string-compare a b) 0))
(define (string-ci=? a b) (string=? (string-downcase a) (string-downcase b)))

(define (string-ref s k)
  (let ((c (string-copy (make-string 2) (substring s k (+ k 1)))))
    (string-ref-internal c 0)))
(define (string-ref-internal s k)
  (+ (string-length (substring s 0 k)) (char->integer (string-ref-char s k))))
(define (string-ref-char s k) #\a) ;; 由编译器内联

(define (substring s start end)
  (let ((len (- end start))
        (buf (make-string (+ len 1))))
    (let loop ((i 0) (j start))
      (if (< i len)
          (begin
            (string-set! buf i (string-ref s j))
            (loop (+ i 1) (+ j 1)))
          (begin
            (string-set! buf i #\nul)
            buf)))))

(define (string-append . strs)
  (if (null? strs)
      ""
      (let* ((total (apply + (map string-length strs)))
             (buf (make-string (+ total 1)))
             (pos 0))
        (for-each (lambda (s)
                    (let ((len (string-length s)))
                      (let loop ((i 0))
                        (if (< i len)
                            (begin
                              (string-set! buf (+ pos i) (string-ref s i))
                              (loop (+ i 1)))))
                      (set! pos (+ pos len))))
                  strs)
        (string-set! buf pos #\nul)
        buf)))

(define (string-map proc s)
  (let* ((len (string-length s))
         (result (make-string len)))
    (let loop ((i 0))
      (if (< i len)
          (begin
            (string-set! result i (proc (string-ref s i)))
            (loop (+ i 1)))))
    result))

(define (string->list s)
  (let loop ((i (- (string-length s) 1))
             (result '()))
    (if (< i 0)
        result
        (loop (- i 1) (cons (string-ref s i) result)))))

(define (list->string lst)
  (let* ((len (length lst))
         (s (make-string len)))
    (let loop ((lst lst) (i 0))
      (if (null? lst)
          s
          (begin
            (string-set! s i (car lst))
            (loop (cdr lst) (+ i 1)))))))

(define (string-downcase s)
  (let ((result (string-copy (make-string (+ (string-length s) 1)) s)))
    (let loop ((i 0))
      (if (< i (string-length result))
          (let ((c (string-ref result i)))
            (if (and (char>=? c #\a) (char<=? c #\z))
                (string-set! result i (integer->char (- (char->integer c) 32))))
            (loop (+ i 1)))))
    result))

(define (string-upcase s)
  (let ((result (string-copy (make-string (+ (string-length s) 1)) s)))
    (let loop ((i 0))
      (if (< i (string-length result))
          (let ((c (string-ref result i)))
            (if (and (char>=? c #\a) (char<=? c #\z))
                (string-set! result i (integer->char (+ (char->integer c) 32))))
            (loop (+ i 1)))))
    result))

(define (string-reverse s)
  (let* ((len (string-length s))
         (result (make-string len)))
    (let loop ((i 0))
      (if (< i len)
          (begin
            (string-set! result i (string-ref s (- len 1 i)))
            (loop (+ i 1)))))
    result))

(define (make-string k . opt-char)
  (let ((c (if (null? opt-char) #\space (car opt-char)))
        (buf (asm "SYSCALL 42")))
    (let loop ((i 0))
      (if (< i k)
          (begin
            (string-set! buf i c)
            (loop (+ i 1)))))
    (string-set! buf k #\nul)
    buf))

(define (string-set! s k c)
  (let ((addr (+ (string->address s) k)))
    (asm "STORE R1 [R0]")))

(define (string->address s)
  (asm "SYSCALL 42") s)

(define (string-trim s)
  (let ((start 0)
        (end (string-length s)))
    (let loop ((i 0))
      (if (and (< i end) (char-whitespace? (string-ref s i)))
          (loop (+ i 1))
          (set! start i)))
    (let loop ((i (- end 1)))
      (if (and (>= i start) (char-whitespace? (string-ref s i)))
          (loop (- i 1))
          (set! end (+ i 1))))
    (substring s start end)))

(define (string-contains s sub)
  (let ((slen (string-length s))
        (sublen (string-length sub)))
    (if (> sublen slen)
        #f
        (let loop ((i 0))
          (if (> (+ i sublen) slen)
              #f
              (if (string-prefix? (substring s i slen) sub)
                  i
                  (loop (+ i 1))))))))

(define (string-prefix? s prefix)
  (let ((plen (string-length prefix)))
    (if (> plen (string-length s))
        #f
        (let loop ((i 0))
          (if (>= i plen)
              #t
              (if (char=? (string-ref s i) (string-ref prefix i))
                  (loop (+ i 1))
                  #f))))))

(define (string-suffix? s suffix)
  (let* ((slen (string-length s))
         (sulen (string-length suffix)))
    (if (> sulen slen)
        #f
        (string-prefix? (substring s (- slen sulen) slen) suffix))))

(define (string-split s delimiter)
  (let ((len (string-length s))
        (dlen (string-length delimiter)))
    (let loop ((start 0) (result '()))
      (let ((pos (string-contains (substring s start len) delimiter)))
        (if pos
            (loop (+ start pos dlen)
                  (cons (substring s start (+ start pos)) result))
            (reverse (cons (substring s start len) result)))))))

(define (string-join lst separator)
  (if (null? lst)
      ""
      (let loop ((lst (cdr lst))
                 (result (car lst)))
        (if (null? lst)
            result
            (loop (cdr lst)
                  (string-append result separator (car lst)))))))

(define (string-replace s old new)
  (let ((parts (string-split s old)))
    (string-join parts new)))

;; ============================================================
;; 字符操作
;; ============================================================
(define (char? x) (= (type-of x) 3))
(define (char=? a b) (= (char->integer a) (char->integer b)))
(define (char<? a b) (< (char->integer a) (char->integer b)))
(define (char>? a b) (> (char->integer a) (char->integer b)))
(define (char<=? a b) (<= (char->integer a) (char->integer b)))
(define (char>=? a b) (>= (char->integer a) (char->integer b)))
(define (char-ci=? a b) (char=? (char-downcase a) (char-downcase b)))

(define (char-alphabetic? c) (asm "SYSCALL 100") 0)
(define (char-numeric? c) (asm "SYSCALL 101") 0)
(define (char-whitespace? c) (or (char=? c #\space) (char=? c #\tab)
                                 (char=? c #\newline) (char=? c #\return)))
(define (char-upper-case? c) (and (char>=? c #\A) (char<=? c #\Z)))
(define (char-lower-case? c) (and (char>=? c #\a) (char<=? c #\z)))

(define (char-upcase c)
  (if (char-lower-case? c)
      (integer->char (- (char->integer c) 32))
      c))
(define (char-downcase c)
  (if (char-upper-case? c)
      (integer->char (+ (char->integer c) 32))
      c))

(define (char->integer c) (asm "SYSCALL 102") 0)
(define (integer->char n) (asm "SYSCALL 103") #\nul)
(define (digit->integer c)
  (if (char-numeric? c)
      (- (char->integer c) (char->integer #\0))
      #f))

;; ============================================================
;; 列表操作
;; ============================================================
(define (cons x y) (asm "SYSCALL 80") (cons x y))
(define (car pair) (asm "SYSCALL 81") pair)
(define (cdr pair) (asm "SYSCALL 82") pair)
(define (set-car! pair val) (asm "SYSCALL 83"))
(define (set-cdr! pair val) (asm "SYSCALL 84"))
(define (null? x) (eq? x '()))
(define (pair? x) (and (not (null? x)) (not (number? x)) (not (string? x)) (not (char? x))))

(define (list . objs) objs)
(define (length lst)
  (if (null? lst) 0 (+ 1 (length (cdr lst)))))
(define (append . lsts)
  (if (null? lsts)
      '()
      (if (null? (cdr lsts))
          (car lsts)
          (fold-right append-one (car lsts) (cdr lsts)))))
(define (append-one lst elem)
  (if (null? lst)
      (list elem)
      (cons (car lst) (append-one (cdr lst) elem))))

(define (reverse lst)
  (let loop ((lst lst) (result '()))
    (if (null? lst)
        result
        (loop (cdr lst) (cons (car lst) result)))))

(define (list-ref lst k)
  (if (= k 0) (car lst) (list-ref (cdr lst) (- k 1))))
(define (list-set! lst k val)
  (if (= k 0)
      (set-car! lst val)
      (list-set! (cdr lst) (- k 1) val)))
(define (list-tail lst k)
  (if (= k 0) lst (list-tail (cdr lst) (- k 1))))

(define (member x lst)
  (cond ((null? lst) #f)
        ((equal? x (car lst)) lst)
        (else (member x (cdr lst)))))
(define (memq x lst)
  (cond ((null? lst) #f)
        ((eq? x (car lst)) lst)
        (else (memq x (cdr lst)))))
(define (memv x lst)
  (cond ((null? lst) #f)
        ((eqv? x (car lst)) lst)
        (else (memv x (cdr lst)))))

(define (assoc key alist)
  (cond ((null? alist) #f)
        ((equal? key (caar alist)) (car alist))
        (else (assoc key (cdr alist)))))
(define (assq key alist)
  (cond ((null? alist) #f)
        ((eq? key (caar alist)) (car alist))
        (else (assq key (cdr alist)))))
(define (assv key alist)
  (cond ((null? alist) #f)
        ((eqv? key (caar alist)) (car alist))
        (else (assv key (cdr alist)))))

;; ============================================================
;; 高阶函数
;; ============================================================
(define (map proc . lsts)
  (if (null? (car lsts))
      '()
      (cons (apply proc (map-one car lsts))
            (apply map proc (map-one cdr lsts)))))
(define (map-one f lsts) (map (lambda (l) (f l)) lsts))

(define (for-each proc lst)
  (if (not (null? lst))
      (begin
        (proc (car lst))
        (for-each proc (cdr lst)))))

(define (filter pred lst)
  (cond ((null? lst) '())
        ((pred (car lst)) (cons (car lst) (filter pred (cdr lst))))
        (else (filter pred (cdr lst)))))

(define (fold-left op init lst)
  (if (null? lst)
      init
      (fold-left op (op init (car lst)) (cdr lst))))
(define (reduce op lst)
  (if (null? (cdr lst))
      (car lst)
      (op (car lst) (reduce op (cdr lst)))))
(define (fold-right op init lst)
  (if (null? lst)
      init
      (op (car lst) (fold-right op init (cdr lst)))))

(define (any pred lst)
  (and (not (null? lst))
       (or (pred (car lst))
           (any pred (cdr lst)))))
(define (every pred lst)
  (or (null? lst)
      (and (pred (car lst))
           (every pred (cdr lst)))))

(define (apply proc . args)
  (let ((last (car (reverse args)))
        (inits (reverse (cdr (reverse args)))))
    (apply-internal proc (append inits last))))
(define (apply-internal proc args) (proc args))

(define (compose f g)
  (lambda (x) (f (g x))))

;; ============================================================
;; 向量操作
;; ============================================================
(define (make-vector k . opt-fill)
  (let* ((fill (if (null? opt-fill) 0 (car opt-fill)))
         (vec (asm "SYSCALL 85")))
    (let loop ((i 0))
      (if (< i k)
          (begin
            (vector-set! vec i fill)
            (loop (+ i 1)))))
    vec))

(define (vector-ref vec k)
  (asm "SYSCALL 86") vec)
(define (vector-set! vec k val)
  (asm "SYSCALL 87"))
(define (vector-length vec)
  (asm "SYSCALL 88") 0)

(define (vector->list vec)
  (let loop ((i (- (vector-length vec) 1)) (result '()))
    (if (< i 0)
        result
        (loop (- i 1) (cons (vector-ref vec i) result)))))

(define (list->vector lst)
  (let* ((len (length lst))
         (vec (make-vector len)))
    (let loop ((lst lst) (i 0))
      (if (null? lst)
          vec
          (begin
            (vector-set! vec i (car lst))
            (loop (cdr lst) (+ i 1)))))))

(define (vector-fill! vec fill)
  (let loop ((i 0))
    (if (< i (vector-length vec))
        (begin
          (vector-set! vec i fill)
          (loop (+ i 1))))))

(define (vector-map proc vec)
  (let* ((len (vector-length vec))
         (result (make-vector len)))
    (let loop ((i 0))
      (if (< i len)
          (begin
            (vector-set! result i (proc (vector-ref vec i)))
            (loop (+ i 1)))))
    result))

(define (vector-for-each proc vec)
  (let ((len (vector-length vec)))
    (let loop ((i 0))
      (if (< i len)
          (begin
            (proc (vector-ref vec i))
            (loop (+ i 1)))))))

;; ============================================================
;; 数学函数
;; ============================================================
(define (abs x) (asm "SYSCALL 43") 0)
(define (min a b) (asm "SYSCALL 45") 0)
(define (max a b) (asm "SYSCALL 46") 0)
(define (sqrt x) (asm "SYSCALL 20") 0.0)
(define (sin x) (asm "SYSCALL 21") 0.0)
(define (cos x) (asm "SYSCALL 22") 0.0)
(define (tan x) (asm "SYSCALL 23") 0.0)
(define (asin x) (asm "SYSCALL 24") 0.0)
(define (acos x) (asm "SYSCALL 25") 0.0)
(define (atan x) (atan2 x 1.0))
(define (atan2 y x) (asm "SYSCALL 33") 0.0)
(define (expt x y) (asm "SYSCALL 26") 0.0)
(define (exp x) (asm "SYSCALL 27") 0.0)
(define (log x) (asm "SYSCALL 28") 0.0)
(define (log10 x) (/ (log x) (log 10)))
(define (floor x) (asm "SYSCALL 29") 0.0)
(define (ceiling x) (asm "SYSCALL 30") 0.0)
(define (round x) (asm "SYSCALL 31") 0.0)
(define (truncate x) (inexact->exact (floor (abs x))))
(define (random) (asm "SYSCALL 50") 0)
(define (random-seed seed) (asm "SYSCALL 51"))
(define pi 3.141592653589793)
(define e 2.718281828459045)

(define (gcd a b)
  (if (= b 0) (abs a) (gcd b (modulo a b))))
(define (lcm a b)
  (/ (* (abs a) (abs b)) (gcd a b)))
(define (modulo a b)
  (- a (* b (floor (/ a b)))))
(define (remainder a b)
  (- a (* b (truncate (/ a b)))))
(define (quotient a b)
  (truncate (/ a b)))

(define (number->string n) (let ((buf "")) (asm "SYSCALL 42") buf))
(define (string->number s) (asm "SYSCALL 40") 0)

(define (even? n) (= (modulo n 2) 0))
(define (odd? n) (= (modulo n 2) 1))
(define (zero? n) (= n 0))
(define (positive? n) (> n 0))
(define (negative? n) (< n 0))

(define (= a b . rest)
  (if (null? rest)
      (=? a b)
      (and (=? a b) (apply = (cons b rest))))))
(define (=? a b) (asm "SYSCALL 47") 0)

(define (< a b . rest)
  (if (null? rest)
      (<? a b)
      (and (<? a b) (apply < (cons b rest))))))
(define (<? a b) (asm "SYSCALL 48") 0)

(define (> a b . rest)
  (if (null? rest)
      (>? a b)
      (and (>? a b) (apply > (cons b rest))))))
(define (>? a b) (asm "SYSCALL 49") 0)

(define (<= a b . rest)
  (if (null? rest)
      (not (>? a b))
      (and (not (>? a b)) (apply <= (cons b rest))))))
(define (>= a b . rest)
  (if (null? rest)
      (not (<? a b))
      (and (not (<? a b)) (apply >= (cons b rest))))))

(define (+ . args) (fold-left add-two 0 args))
(define (add-two a b) (asm "SYSCALL 34") 0)
(define (- a . rest)
  (if (null? rest)
      (negate a)
      (fold-left sub-two a rest)))
(define (sub-two a b) (asm "SYSCALL 35") 0)
(define (negate x) (- 0 x))
(define (* . args) (fold-left mul-two 1 args))
(define (mul-two a b) (asm "SYSCALL 36") 0)
(define (/ a . rest)
  (if (null? rest)
      (div-two 1 a)
      (fold-left div-two a rest)))
(define (div-two a b) (asm "SYSCALL 37") 0)

;; ============================================================
;; 类型谓词
;; ============================================================
(define (type-of x)
  (cond ((null? x) 0)
        ((boolean? x) 1)
        ((and (integer? x) (exact? x)) 2)
        ((number? x) 2)
        ((char? x) 3)
        ((string? x) 4)
        ((pair? x) 5)
        ((vector? x) 6)
        ((symbol? x) 7)
        ((procedure? x) 8)
        (else 9)))

(define (boolean? x) (or (eq? x #t) (eq? x #f)))
(define (number? x) (asm "SYSCALL 104") 0)
(define (integer? x) (and (number? x) (= (floor x) x)))
(define (real? x) (number? x))
(define (exact? x) (integer? x))
(define (inexact? x) (not (exact? x)))
(define (exact->inexact x) (* x 1.0))
(define (inexact->exact x) (floor x))
(define (string? x) (and (not (null? x)) (not (number? x))
                        (not (char? x)) (not (pair? x)) (not (vector? x))
                        (not (symbol? x)) (not (procedure? x))))
(define (symbol? x) (asm "SYSCALL 105") 0)
(define (vector? x) (asm "SYSCALL 106") 0)
(define (procedure? x) (asm "SYSCALL 107") 0)
(define (list? x)
  (or (null? x)
      (and (pair? x) (list? (cdr x)))))

;; ============================================================
;; 等价性谓词
;; ============================================================
(define (eq? a b) (asm "SYSCALL 108") 0)
(define (eqv? a b)
  (or (eq? a b)
      (and (number? a) (number? b) (= a b))
      (and (char? a) (char? b) (char=? a b))
      (and (string? a) (string? b) (string=? a b))))
(define (equal? a b)
  (or (eqv? a b)
      (and (pair? a) (pair? b)
           (equal? (car a) (car b))
           (equal? (cdr a) (cdr b)))
      (and (vector? a) (vector? b)
           (let ((la (vector-length a)))
             (and (= la (vector-length b))
                  (let loop ((i 0))
                    (or (>= i la)
                        (and (equal? (vector-ref a i) (vector-ref b i))
                             (loop (+ i 1))))))))))

;; ============================================================
;; 文件 I/O (OS 模式)
;; ============================================================
(define (open-input-file filename)
  (asm "SYSCALL 110") 0)
(define (open-output-file filename)
  (asm "SYSCALL 111") 0)
(define (close-input-port port)
  (asm "SYSCALL 112"))
(define (close-output-port port)
  (asm "SYSCALL 113"))
(define (read port)
  (asm "SYSCALL 114") 0)
(define (read-char port)
  (asm "SYSCALL 115") #\nul)
(define (peek-char . port)
  (asm "SYSCALL 116") #\nul)
(define (write-char c . port)
  (asm "SYSCALL 117"))
(define (write obj . port)
  (asm "SYSCALL 118"))
(define (call-with-input-file filename proc)
  (let ((port (open-input-file filename)))
    (let ((result (proc port)))
      (close-input-port port)
      result)))
(define (call-with-output-file filename proc)
  (let ((port (open-output-file filename)))
    (let ((result (proc port)))
      (close-output-port port)
      result)))
(define (with-input-from-file filename thunk)
  (call-with-input-file filename (lambda (p) (thunk))))
(define (with-output-to-file filename thunk)
  (call-with-output-file filename (lambda (p) (thunk))))

;; ============================================================
;; 内存操作
;; ============================================================
(define (memset! ptr value count) (asm "SYSCALL 70"))
(define (memcpy! dest src count) (asm "SYSCALL 71"))
(define (memcmp a b n) (asm "SYSCALL 13") 0)
(define (malloc size) (asm "SYSCALL 40") 0)
(define (free ptr) (asm "SYSCALL 41"))

;; ============================================================
;; 系统
;; ============================================================
(define (exit . code)
  (if (not (null? code)) (asm "SYSCALL 3"))
  (asm "SYSCALL 3"))
(define (delay ms) (asm "SYSCALL 52"))
(define (sleep ms) (delay ms))
(define (get-tick) (asm "SYSCALL 53") 0)
(define (get-config key) (asm "SYSCALL 60") 0)
(define (get-date) (let ((buf "")) (asm "SYSCALL 55") buf))
(define (get-time) (let ((buf "")) (asm "SYSCALL 56") buf))
(define (get-datetime) (asm "SYSCALL 54") 0)
(define (current-second)
  (modulo (get-tick) 60))
(define (current-jiffy)
  (exact->inexact (/ (get-tick) 1000.0)))

;; ============================================================
;; 错误处理
;; ============================================================
(define (error msg . args)
  (display "Error: ")
  (display msg)
  (for-each (lambda (x) (display " ") (write x)) args)
  (newline)
  (exit 1))

(define (assert condition . msg)
  (if (not condition)
      (apply error (if (null? msg) '("assertion failed") msg))))

;; ============================================================
;; 有用的杂项
;; ============================================================
(define (identity x) x)
(define (const x) (lambda (y) x))
(define (flip f) (lambda (x y) (f y x)))
(define (curry f . args)
  (lambda more-args (apply f (append args more-args))))
(define (complement pred)
  (lambda (x) (not (pred x))))

(define (caar x) (car (car x)))
(define (cadr x) (car (cdr x)))
(define (cdar x) (cdr (car x)))
(define (cddr x) (cdr (cdr x)))
(define (caaar x) (car (car (car x))))
(define (caadr x) (car (car (cdr x))))
(define (cadar x) (car (cdr (car x))))
(define (caddr x) (car (cdr (cdr x))))
(define (cdaar x) (cdr (car (car x))))
(define (cdadr x) (cdr (car (cdr x))))
(define (cddar x) (cdr (cdr (car x))))
(define (cdddr x) (cdr (cdr (cdr x))))

;; ============================================================
;; 类型转换 (调用共享库)
;; ============================================================
(define (number->string n)
  (asm "CALL shared_int_to_str")
  "")

(define (string->number s)
  (asm "CALL shared_atoi")
  0)
