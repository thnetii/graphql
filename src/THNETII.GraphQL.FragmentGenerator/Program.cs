using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL.Client;
using GraphQL.Common.Response;

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
            var options = new[]
            {
                new Option(new []{"--method", "-m"}, "HTTP Method to use", new Argument<GraphQLClientMethod>(GraphQLClientMethod.Post))
            };
            var rootCommand = new RootCommand(description, options)
            {
                Argument = new Argument<Uri>
                {
                    Name = "uri",
                    Description = "GraphQL Endpoint",
                    Arity = ArgumentArity.ExactlyOne
                },
                Handler = CommandHandler.Create(async (Uri uri, GraphQLClientMethod method) =>
                {
                    using (var client = new GraphQLClient(uri))
                    {
                        GraphQLResponse response;
                        switch (method)
                        {
                            case GraphQLClientMethod.Get:
                                response = await client.GetIntrospectionQueryAsync();
                                break;
                            default:
                            case GraphQLClientMethod.Post:
                                response = await client.PostIntrospectionQueryAsync();
                                break;
                        }

                    }
                })
            };
            return rootCommand.InvokeAsync(args);
        }
    }
}
