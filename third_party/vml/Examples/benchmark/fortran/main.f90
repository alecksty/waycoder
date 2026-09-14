program benchmark
implicit none
integer :: i,a,b
a=0;b=1
do i=0,9999; a=a+i; a=a-1; end do
do i=1,9999; b=b*i; b=b/i; end do
end program
