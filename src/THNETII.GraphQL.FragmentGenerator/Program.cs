using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using GraphQL.Client.Http;

using Newtonsoft.Json;

namespace THNETII.GraphQL.FragmentGenerator
{
    public static class Program
    {
        internal static readonly ICommandHandler Handler = CommandHandler.Create(
        async (Uri uri, CancellationToken cancelToken) =>
        {
            using var client = new GraphQLHttpClient(uri);
            var response = await client
                .SendQueryAsync<object>(IntrospectionRequest, cancelToken)
                .ConfigureAwait(false);
            string responseJson = JsonConvert.SerializeObject(response, Formatting.Indented);
            await Console.Out.WriteLineAsync(responseJson.AsMemory(), cancelToken)
                .ConfigureAwait(false);
        });

        public static Task<int> Main(string[] args)
        {
            string description = typeof(Program).Assembly
                .GetCustomAttribute<AssemblyDescriptionAttribute>()?
                .Description;
            var command = new RootCommand(description) { Handler = Handler };
            var parser = new CommandLineBuilder(command)
                .AddArgument(new Argument<Uri>
                {
                    Name = "uri",
                    Description = "GraphQL Endpoint",
                    Arity = ArgumentArity.ExactlyOne
                })
                .CancelOnProcessTermination()
                .UseDefaults()
                .Build();
            return parser.InvokeAsync(args ?? Array.Empty<string>());
        }

        public static GraphQLHttpRequest IntrospectionRequest { get; } =
            new GraphQLHttpRequest(IntrospectionQuery);

        public const string IntrospectionQuery = @"query IntrospectionQuery {
    __schema {
      queryType { name }
      mutationType { name }
      subscriptionType { name }
      types {
        ...FullType
      }
      directives {
        name
        description
        args {
          ...InputValue
        }
        onOperation
        onFragment
        onField
      }
    }
  }

  fragment FullType on __Type {
    kind
    name
    description
    fields(includeDeprecated: true) {
      name
      description
      args {
        ...InputValue
      }
      type {
        ...TypeRef
      }
      isDeprecated
      deprecationReason
    }
    inputFields {
      ...InputValue
    }
    interfaces {
      ...TypeRef
    }
    enumValues(includeDeprecated: true) {
      name
      description
      isDeprecated
      deprecationReason
    }
    possibleTypes {
      ...TypeRef
    }
  }

  fragment InputValue on __InputValue {
    name
    description
    type { ...TypeRef }
    defaultValue
  }

  fragment TypeRef on __Type {
    kind
    name
    ofType {
      kind
      name
      ofType {
        kind
        name
        ofType {
          kind
          name
        }
      }
    }
  }";
    }
}
