﻿using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Reflection;
using System.Threading.Tasks;

using GraphQL.Client;

namespace THNETII.GraphQL.FragmentGenerator
{
    class Program
    {
        enum GraphQLClientMethod
        {
            Get,
            Post
        }

        static Task<int> Main(string[] args)
        {
            string description = typeof(Program).Assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description;
            var rootCommand = new RootCommand(description)
            {
                new Argument<Uri>
                {
                    Name = "uri",
                    Description = "GraphQL Endpoint",
                    Arity = ArgumentArity.ExactlyOne
                },
                new Option<GraphQLClientMethod>(new []{"--method", "-m"}, () => GraphQLClientMethod.Post, "HTTP Method to use")
            };
            rootCommand.Handler = CommandHandler.Create(async (Uri uri, GraphQLClientMethod method) =>
            {
                using var client = new GraphQLClient(uri);
                var response = await (method switch
                {
                    GraphQLClientMethod.Get => client.GetIntrospectionQueryAsync(),
                    _ => client.PostIntrospectionQueryAsync(),
                }).ConfigureAwait(false);
            });
            return rootCommand.InvokeAsync(args);
        }
    }
}
