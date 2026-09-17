using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GenLib;

/// <summary>System.Text.Json 源生成器上下文 — Native AOT 兼容</summary>
[JsonSerializable(typeof(Dictionary<string, ModuleDef>))]
[JsonSerializable(typeof(ModuleDef))]
[JsonSerializable(typeof(Dictionary<string, LanguageNaming>))]
[JsonSerializable(typeof(LanguageNaming))]
[JsonSerializable(typeof(AliasDef))]
internal partial class GenLibJsonContext : JsonSerializerContext { }
