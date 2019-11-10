using System;
using System.Collections.Generic;

namespace THNETII.GraphQL.Http
{
    /// <summary>
	/// Represents the location where the <see cref="GraphQLError"/> has been found
	/// </summary>
	public class GraphQLLocation : IEquatable<GraphQLLocation>
    {

        /// <summary>
        /// The Column
        /// </summary>
        public uint Column { get; set; }

        /// <summary>
        /// The Line
        /// </summary>
        public uint Line { get; set; }

        /// <inheritdoc />
        public override bool Equals(object obj) => Equals(obj as GraphQLLocation);

        /// <inheritdoc />
        public bool Equals(GraphQLLocation other)
        {
            return other switch
            {
                null => false,
                GraphQLLocation l when ReferenceEquals(this, l) => true,
                GraphQLLocation l when Column != l.Column => false,
                GraphQLLocation l when Line != l.Line => false,
                _ => true,
            };
        }

        /// <inheritdoc />
        public override int GetHashCode() => EqualityComparer<GraphQLLocation>.Default.GetHashCode(this);

        /// <inheritdoc />
        public static bool operator ==(GraphQLLocation location1, GraphQLLocation location2)
        {
            if (location1 is null)
                return location2 is null ? true : false;
            return location1.Equals(location2);
        }

        /// <inheritdoc />
        public static bool operator !=(GraphQLLocation location1, GraphQLLocation location2)
        {
            if (location1 is null)
                return location2 is null ? false : true;
            return !location1.Equals(location2);
        }
    }
}
