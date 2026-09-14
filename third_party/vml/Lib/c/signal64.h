/* signal64.h - 64-bit Signal Processing */

#ifndef _SIGNAL64_H
#define _SIGNAL64_H

#param lib("signal64")

long lmoving_avg_init64(long window_size, long* buffer, long* state);
long lmoving_avg_update64(long* state, long new_value);
long lema_init64(long* state);
long lema_update64(long* state, long value, long alpha);
void lkalman_init64(long* state, long initial_value, long process_noise, long measure_noise);
long lkalman_update64(long* state, long measurement);
long ldeadband64(long value, long threshold);
long lhysteresis64(long* state, long input, long on_threshold, long off_threshold);

#endif /* _SIGNAL64_H */
