﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.Tests.Utility;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.CommandLine.Tests;

public class ParseErrorReportingTests
{
    [Fact] // https://github.com/dotnet/command-line-api/issues/817
    public void Parse_error_reporting_reports_error_when_help_is_used_and_required_subcommand_is_missing()
    {
        var root = new RootCommand
        {
            new Command("inner"),
            new HelpOption()
        };

        var output = new StringWriter();
        var parseResult = root.Parse("");

        parseResult.Errors.Should().NotBeEmpty();

        var result = parseResult.Invoke(new() { Output = output });

        result.Should().Be(1);
        output.ToString().Should().ShowHelp();
    }

    [Fact]
    public void Help_display_can_be_disabled()
    {
        RootCommand rootCommand = new()
        {
            new Option<bool>("--verbose")
        };

        var output = new StringWriter();

        var result = rootCommand.Parse("oops");

        if (result.Action is ParseErrorAction parseError)
        {
            parseError.ShowHelp = false;
        }

        result.Invoke(new() { Output = output });

        output.ToString().Should().NotShowHelp();
    }

    [Theory] // https://github.com/dotnet/command-line-api/issues/2226
    [InlineData(true)]
    [InlineData(false)]
    public void When_there_are_parse_errors_then_customized_help_action_is_used_if_present(bool useAsyncAction)
    {
        var wasCalled = false;
        RootCommand rootCommand = new();
        rootCommand.Options.Clear();
        CommandLineAction customHelpAction = useAsyncAction
                                         ? new AsynchronousTestAction(_ => wasCalled = true)
                                         : new SynchronousTestAction(_ => wasCalled = true);

        rootCommand.Add(new HelpOption
        {
            Action = customHelpAction
        });

        rootCommand.Parse("oops").Invoke();

        wasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task When_there_are_parse_errors_then_customized_help_action_on_ancestor_is_used_if_present()
    {
        bool rootHelpWasCalled = false;

        var rootCommand = new RootCommand
        {
            new Command("child")
            {
                new Command("grandchild")
            }
        };

        rootCommand.Options.OfType<HelpOption>().Single().Action = new SynchronousTestAction(_ =>
        {
            rootHelpWasCalled = true;
        });

        await rootCommand.Parse("child grandchild oops").InvokeAsync();

        rootHelpWasCalled.Should().BeTrue();
    }

    [Fact]
    public void When_no_help_option_is_present_then_help_is_not_shown_for_parse_errors()
    {
        RootCommand rootCommand = new();
        rootCommand.Options.Clear();
        var output = new StringWriter();
        
        rootCommand.Parse("oops").Invoke(new() { Output = output } );

        output.ToString().Should().NotShowHelp();
    }
}