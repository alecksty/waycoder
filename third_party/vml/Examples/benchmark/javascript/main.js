// VML性能基准 — JavaScript (纯整数)
function main() {
    var a=0,b=1;
    for(var i=0;i<10000;i++){a=a+i;a=a-1;}
    for(var i=1;i<10000;i++){b=b*i/i;}
    return a+b;
}
