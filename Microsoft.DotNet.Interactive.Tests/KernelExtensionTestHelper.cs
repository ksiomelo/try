﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.Tests
{
    internal static class KernelExtensionTestHelper
    {
        internal static async Task<FileInfo> CreateExtensionInDirectory(
            DirectoryInfo extensionDir, 
            string body, 
            DirectoryInfo outputDir, 
            [CallerMemberName] string testName = null)
        {
            var extensionDll = await CreateExtension(extensionDir, body, testName);

            if (!outputDir.Exists)
            {
                outputDir.Create();
            }

            var finalExtensionDll = new FileInfo(Path.Combine(outputDir.FullName, extensionDll.Name));
            File.Copy(extensionDll.FullName, finalExtensionDll.FullName);

            return finalExtensionDll;
        }

        internal static async Task<FileInfo> CreateExtension(
            DirectoryInfo extensionDir, 
            string body, 
            [CallerMemberName] string extensionName = null)
        {
            var microsoftDotNetInteractiveDllPath = typeof(IKernelExtension).Assembly.Location;

            extensionDir.Populate(
                ("Extension.cs", $@"
using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;

public class TestKernelExtension : IKernelExtension
{{
    public async Task OnLoadAsync(IKernel kernel)
    {{
        {body}
    }}
}}
"),
                ("TestExtension.csproj", $@"
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>netcoreapp3.0</TargetFramework>
    <AssemblyName>{extensionName}</AssemblyName>
  </PropertyGroup>

    <ItemGroup>

    <Reference Include=""Microsoft.DotNet.Interactive"">
      <HintPath>{microsoftDotNetInteractiveDllPath}</HintPath>
    </Reference>
  </ItemGroup>

</Project>
"));

            var buildResult = await new Dotnet(extensionDir).Build();
            buildResult.ThrowOnFailure();

            return extensionDir
                               .GetDirectories("bin", SearchOption.AllDirectories)
                               .Single()
                               .GetFiles($"{extensionName}.dll", SearchOption.AllDirectories)
                               .Single();
        }
    }
}