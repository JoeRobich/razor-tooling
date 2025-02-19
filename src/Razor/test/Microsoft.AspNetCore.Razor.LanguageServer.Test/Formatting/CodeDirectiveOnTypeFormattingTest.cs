﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    public class CodeDirectiveOnTypeFormattingTest : FormattingTestBase
    {
        public CodeDirectiveOnTypeFormattingTest(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact]
        public async Task CloseCurly_Class_SingleLineAsync()
        {
            await RunOnTypeFormattingTestAsync(
input: @"
@code {
 public class Foo{}$$
}
",
expected: @"
@code {
    public class Foo { }
}
", triggerCharacter: '}');
        }

        [Fact]
        public async Task CloseCurly_Class_SingleLine_UseTabsAsync()
        {
            await RunOnTypeFormattingTestAsync(
input: @"
@code {
 public class Foo{}$$
}
",
expected: @"
@code {
	public class Foo { }
}
", triggerCharacter: '}', insertSpaces: false);
        }

        [Fact]
        public async Task CloseCurly_Class_SingleLine_AdjustTabSizeAsync()
        {
            await RunOnTypeFormattingTestAsync(
input: @"
@code {
 public class Foo{}$$
}
",
expected: @"
@code {
      public class Foo { }
}
", triggerCharacter: '}', tabSize: 6);
        }

        [Fact]
        public async Task CloseCurly_Class_MultiLineAsync()
        {
            await RunOnTypeFormattingTestAsync(
input: @"
@code {
 public class Foo{
}$$
}
",
expected: @"
@code {
    public class Foo
    {
    }
}
", triggerCharacter: '}');
        }

        [Fact]
        public async Task CloseCurly_Method_SingleLineAsync()
        {
            await RunOnTypeFormattingTestAsync(
input: @"
@code {
 public void Foo{}$$
}
",
expected: @"
@code {
    public void Foo { }
}
", triggerCharacter: '}');
        }

        [Fact]
        public async Task CloseCurly_Method_MultiLineAsync()
        {
            await RunOnTypeFormattingTestAsync(
input: @"
@code {
 public void Foo{
}$$
}
",
expected: @"
@code {
    public void Foo
    {
    }
}
", triggerCharacter: '}');
        }

        [Fact]
        public async Task CloseCurly_Property_SingleLineAsync()
        {
            await RunOnTypeFormattingTestAsync(
input: @"
@code {
 public string Foo{ get;set;}$$
}
",
expected: @"
@code {
    public string Foo { get; set; }
}
", triggerCharacter: '}');
        }

        [Fact]
        public async Task CloseCurly_Property_MultiLineAsync()
        {
            await RunOnTypeFormattingTestAsync(
input: @"
@code {
 public string Foo{
get;set;}$$
}
",
expected: @"
@code {
    public string Foo
    {
        get; set;
    }
}
", triggerCharacter: '}');
        }

        [Fact]
        public async Task CloseCurly_Property_StartOfBlockAsync()
        {
            await RunOnTypeFormattingTestAsync(
input: @"
@code { public string Foo{ get;set;}$$
}
",
expected: @"
@code {
    public string Foo { get; set; }
}
", triggerCharacter: '}');
        }

        [Fact]
        public async Task Semicolon_ClassField_SingleLineAsync()
        {
            await RunOnTypeFormattingTestAsync(
input: @"
@code {
 public class Foo {private int _hello = 0;$$}
}
",
expected: @"
@code {
    public class Foo { private int _hello = 0; }
}
", triggerCharacter: ';');
        }

        [Fact]
        public async Task Semicolon_ClassField_MultiLineAsync()
        {
            await RunOnTypeFormattingTestAsync(
input: @"
@code {
    public class Foo{
private int _hello = 0;$$ }
}
",
expected: @"
@code {
    public class Foo{
        private int _hello = 0; }
}
", triggerCharacter: ';');
        }

        [Fact]
        public async Task Semicolon_MethodVariableAsync()
        {
            await RunOnTypeFormattingTestAsync(
input: @"
@code {
    public void Foo()
    {
                            var hello = 0;$$
    }
}
",
expected: @"
@code {
    public void Foo()
    {
        var hello = 0;
    }
}
", triggerCharacter: ';');
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/aspnetcore/issues/27135")]
        public async Task Semicolon_Fluent_CallAsync()
        {
            await RunOnTypeFormattingTestAsync(
input: @"@implements IDisposable

@code{
    protected override async Task OnInitializedAsync()
    {
        hubConnection = new HubConnectionBuilder()
            .WithUrl(NavigationManager.ToAbsoluteUri(""/chathub""))
            .Build();$$
    }
}
",
expected: @"@implements IDisposable

@code{
    protected override async Task OnInitializedAsync()
    {
        hubConnection = new HubConnectionBuilder()
            .WithUrl(NavigationManager.ToAbsoluteUri(""/chathub""))
            .Build();
    }
}
", triggerCharacter: ';');
        }

        [Fact]
        public async Task ClosingBrace_MatchesCSharpIndentationAsync()
        {
            await RunOnTypeFormattingTestAsync(
input: @"
@page ""/counter""

<h1>Counter</h1>

<p>Current count: @currentCount</p>

<button class=""btn btn-primary"" @onclick=""IncrementCount"">Click me</button>

@code {
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
        if (true){
            }$$
    }
}
",
expected: @"
@page ""/counter""

<h1>Counter</h1>

<p>Current count: @currentCount</p>

<button class=""btn btn-primary"" @onclick=""IncrementCount"">Click me</button>

@code {
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
        if (true)
        {
        }
    }
}
", triggerCharacter: '}');
        }

        [Fact]
        public async Task ClosingBrace_DoesntMatchCSharpIndentationAsync()
        {
            await RunOnTypeFormattingTestAsync(
input: @"
@page ""/counter""

<h1>Counter</h1>

<p>Current count: @currentCount</p>

<button class=""btn btn-primary"" @onclick=""IncrementCount"">Click me</button>

@code {
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
        if (true){
                }$$
    }
}
",
expected: @"
@page ""/counter""

<h1>Counter</h1>

<p>Current count: @currentCount</p>

<button class=""btn btn-primary"" @onclick=""IncrementCount"">Click me</button>

@code {
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
        if (true)
        {
        }
    }
}
", triggerCharacter: '}');
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/aspnetcore/issues/27102")]
        public async Task CodeBlock_SemiColon_SingleLine1Async()
        {
            await RunOnTypeFormattingTestAsync(
input: @"
<div></div>
@{ Debugger.Launch();$$}
<div></div>
",
expected: @"
<div></div>
@{
    Debugger.Launch();
}
<div></div>
", triggerCharacter: ';');
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/aspnetcore/issues/27102")]
        public async Task CodeBlock_SemiColon_SingleLine2Async()
        {
            await RunOnTypeFormattingTestAsync(
input: @"
<div></div>
@{     Debugger.Launch(   )     ;$$ }
<div></div>
",
expected: @"
<div></div>
@{
    Debugger.Launch(); 
}
<div></div>
", triggerCharacter: ';');
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/aspnetcore/issues/27102")]
        public async Task CodeBlock_SemiColon_SingleLine3Async()
        {
            await RunOnTypeFormattingTestAsync(
input: @"
<div>
    @{     Debugger.Launch(   )     ;$$ }
</div>
",
expected: @"
<div>
    @{
        Debugger.Launch(); 
    }
</div>
", triggerCharacter: ';');
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/aspnetcore/issues/27102")]
        public async Task CodeBlock_SemiColon_MultiLineAsync()
        {
            await RunOnTypeFormattingTestAsync(
input: @"
<div></div>
@{
    var abc = 123;$$
}
<div></div>
",
expected: @"
<div></div>
@{
    var abc = 123;
}
<div></div>
", triggerCharacter: ';');
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/aspnetcore/issues/34319")]
        public async Task Switch_Statment_NestedHtml_NestedCodeBlock()
        {
            await RunOnTypeFormattingTestAsync(
input: @"
@switch (""asdf"")
{
    case ""asdf"":
        <div>
            @if (true)
            {
                <strong></strong>
            }
            else if (false)
            {
1.ToString();$$
            }
        </div>
        break;
}
",
expected: @"
@switch (""asdf"")
{
    case ""asdf"":
        <div>
            @if (true)
            {
                <strong></strong>
            }
            else if (false)
            {
                1.ToString();
            }
        </div>
        break;
}
", triggerCharacter: ';');
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/aspnetcore/issues/34319")]
        public async Task NestedHtml_NestedCodeBlock()
        {
            await RunOnTypeFormattingTestAsync(
input: @"
@if (true)
{
    <div>
        @if (true)
        {
            <strong></strong>
        }
        else if (false)
        {
1.ToString();$$
        }
    </div>
}
",
expected: @"
@if (true)
{
    <div>
        @if (true)
        {
            <strong></strong>
        }
        else if (false)
        {
            1.ToString();
        }
    </div>
}
", triggerCharacter: ';');
        }

        [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/36390")]
        [WorkItem("https://github.com/dotnet/aspnetcore/issues/34319")]
        public async Task NestedHtml_NestedCodeBlock_EndingBrace()
        {
            await RunOnTypeFormattingTestAsync(
input: @"
@if (true)
{
    <div>
        @if (true)
        {
            <strong></strong>
        }
        else if (false)
        {
            }$$
    </div>
}
",
expected: @"
@if (true)
{
    <div>
        @if (true)
        {
            <strong></strong>
        }
        else if (false)
        {
        }
    </div>
}
", triggerCharacter: '}');
        }

        [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/36390")]
        [WorkItem("https://github.com/dotnet/aspnetcore/issues/34319")]
        public async Task NestedHtml_NestedCodeBlock_EndingBrace_WithCode()
        {
            await RunOnTypeFormattingTestAsync(
input: @"
@if (true)
{
    <div>
        @if (true)
        {
            <strong></strong>
        }
        else if (false)
        {
            ""asdf"".ToString();
            }$$
    </div>
}
",
expected: @"
@if (true)
{
    <div>
        @if (true)
        {
            <strong></strong>
        }
        else if (false)
        {
            ""asdf"".ToString();
        }
    </div>
}
", triggerCharacter: '}');
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/razor-tooling/issues/5698")]
        public async Task Semicolon_NoDocumentChanges()
        {
            var input = @"
@page ""/""

@code {
    void Foo()
    {
        DateTime.Now;$$
    }
}
";

            await RunOnTypeFormattingTestAsync(input, input.Replace("$$", ""), triggerCharacter: ';');
        }

        [Fact(Skip = "https://github.com/dotnet/razor-tooling/issues/5676")]
        [WorkItem("https://github.com/dotnet/razor-tooling/issues/5693")]
        public async Task IfStatementInsideLambda()
        {
            await RunOnTypeFormattingTestAsync(
input: @"
@code
{
    public RenderFragment RenderFoo()
    {
        return (__builder) =>
        {
            @if (true)
            {

            }$$
        };
    }
}
",
expected: @"
@code
{
    public RenderFragment RenderFoo()
    {
        return (__builder) =>
        {
            @if (true)
            {

            }
        };
    }
}
", triggerCharacter: '}');
        }
    }
}
