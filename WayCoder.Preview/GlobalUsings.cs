// 共享源码（主项目 ImplicitUsings=enable）依赖隐式 using；WPF 临时工程不自动提供，
// 这里显式补全 SDK 默认隐式 using 集，保证 Config/Global 等共享文件编译通过。
global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Net.Http;
global using System.Threading;
global using System.Threading.Tasks;
// 主项目 Config/GlobalUsings.cs 的全局 using（共享源码依赖）
global using WayCoder.Infra;
global using System.Runtime.InteropServices;
