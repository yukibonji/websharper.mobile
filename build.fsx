// WebSharper.Mobile - support for building mobile WebSharper apps
// Copyright (c) 2013 IntelliFactory
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#r "System.Xml"
#r "System.Xml.Linq"
#load "src/VisualStudioIntegration.fsx"

open System
open System.Diagnostics
open System.IO
open IntelliFactory.Build
open global.NuGet

let bt = BuildTool().PackageId("WebSharper.Mobile", "2.5-rc1")

let mobile =
    bt.WebSharper.Library("IntelliFactory.WebSharper.Mobile")
        .SourcesFromProject()

let winPhone =
    bt.WebSharper.Library("IntelliFactory.WebSharper.WinPhone")
        .SourcesFromProject()
        .References(fun r ->
            [
                r.Project(mobile)
                r.Assembly("System.Web")
            ])
        .Embed(["Runtime.js"])

let android =
    bt.WebSharper.Library("IntelliFactory.WebSharper.Android")
        .SourcesFromProject()
        .References(fun r ->
            [
                r.Project(mobile)
                r.Assembly("System.Web")
            ])
        .Embed(["base64_support.js"])

let msb =
    bt.MSBuild("automation/MobileTemplates.proj")

let file target path =
    let path = Path.Combine(__SOURCE_DIRECTORY__, path)
    {
        new INuGetFile with
            member x.Read() = File.OpenRead(path) :> _
            member x.TargetPath = Path.Combine(target, Path.GetFileName(path))
    }

let buildAndroidJar () =
    let psi = ProcessStartInfo()
    psi.Arguments <- "release"
    psi.CreateNoWindow <- true
    let ah =
        match Environment.GetEnvironmentVariable("ANT_HOME") with
        | null -> failwith "ANT_HOME not set"
        | ah -> ah
    psi.FileName <- Path.Combine(ah, "bin", "ant.bat")
    psi.UseShellExecute <- false
    psi.WindowStyle <- ProcessWindowStyle.Hidden
    let wat = Path.Combine(__SOURCE_DIRECTORY__, "websharper-android-tools")
    psi.WorkingDirectory <- wat
    let p = Process.Start(psi)
    p.WaitForExit()
    if p.ExitCode <> 0 then
        failwith "Invalid exit code: %i" p.ExitCode
    {
        new INuGetFile with
            member x.Read() = File.OpenRead(Path.Combine(wat, "bin", "classes.jar")) :> _
            member x.TargetPath = "build/websharper-android-tools.jar"
    }

let nuPkg =
    let nuPkg =
        bt.NuGet.CreatePackage()
            .Configure(fun x ->
                {
                    x with
                        Description = "Mobile app development with WebSharper"
                        ProjectUrl = Some "http://github.com/intellifactory/websharper.mobile"
                        LicenseUrl = Some "http://github.com/intellifactory/websharper.mobile/blob/master/LICENSE.md"
                        RequiresLicenseAcceptance = true
                })
    let android = buildAndroidJar ()
    nuPkg.AddNuGetExportingProject {
        new INuGetExportingProject with
            member p.NuGetFiles =
                seq {
                    yield file "build" "automation/WebSharper.Mobile.targets"
                    yield file "build" "build/net45/IntelliFactory.WebSharper.Android.dll"
                    yield file "build" "build/net45/IntelliFactory.WebSharper.Mobile.dll"
                    yield file "build" "build/net45/IntelliFactory.WebSharper.WinPhone.dll"
                    yield file "build" "build/net45/IntelliFactory.WebSharper.WinPhone.Tasks.dll"
                    yield file "build" "build/wp71/IntelliFactory.WebSharper.WinPhone.Runtime.dll"
                    yield android
                }
    }

bt.Solution [
    mobile
    winPhone
    android
    msb
    nuPkg
]
|> bt.Dispatch

do
    let nuPkgPath = nuPkg.GetComputedFileName()
    let vsixPath = Path.ChangeExtension(nuPkgPath, ".vsix")
    VisualStudioIntegration.BuildVsix {
        MobileNuPkgPath = nuPkgPath
        RootPath = __SOURCE_DIRECTORY__
        VsixPath = vsixPath
        WebSharperNuPkgPath =
            let pm = NuGetConfig.CurrentPackageManager.Find(bt)
            let ws = pm.LocalRepository.FindPackage("WebSharper")
            let path = pm.PathResolver.GetPackageFileName(ws)
            let dir = pm.PathResolver.GetInstallPath(ws)
            let path = Path.Combine(dir, path)
            path
    }

