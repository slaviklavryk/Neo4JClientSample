using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4jClient;
using Newtonsoft.Json;

namespace Neo4JClient_Test
{
    public class Movie
    {
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "released")]
        public int Released { get; set; }

        [JsonProperty(PropertyName = "tagline")]
        public string TagLine { get; set; }
    }

    public class Person
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "born")]
        public int YearBorn { get; set; }
     }

    class Program
    {

        static void Main(string[] args)
        {
            var client = new GraphClient(new Uri("http://localhost:7474/db/data/"), "neo4j", "movietest");

            client.Connect();

            //select arbitrary 10 movies
            //MATCH (m:Movie) RETURN m LIMIT 10
            var movies = client.Cypher
                  .Match("(m:Movie)")
                  .Return(m => m.As<Movie>())
                  .Limit(10)
                  .Results;

            //select arbitrary 10 persons
            //MATCH (p:Person) RETURN p LIMIT 10
            var persons = client.Cypher
                  .Match("(p:Person)")
                  .Return(p => p.As<Person>())
                  .Limit(10)
                  .Results;

            //return two results without "collect" aggregation
            //MATCH (actor:Person {name: 'Tom Hanks'})-[:ACTED_IN]->(actorMovies) RETURN actor, actorMovies
            var actorAndMovies = client.Cypher
                  .Match("(actor:Person {name: {actorName}})-[:ACTED_IN]->(actorMovies)")
                  .WithParam("actorName", "Tom Hanks")
                  .Return((actor, actorMovies) => new {
                      ActorPerson = actor.As<Person>(),
                      ActorMovies = actorMovies.As<Movie>()
                  })
                  .Results;

            //return two results with "collect" aggregation
            //MATCH (actor:Person {name: 'Tom Hanks'})-[:ACTED_IN]->(actorMovies) RETURN actor, collect(actorMovies)
            var actorAndMovies2 = client.Cypher
                  .Match("(actor:Person {name: {actorName}})-[:ACTED_IN]->(actorMovies)")
                  .WithParam("actorName", "Tom Hanks")
                  .Return((actor, actorMovies) => new {
                      ActorPerson = actor.As<Person>(),
                      ActorMovies = actorMovies.CollectAs<Movie>()
                  })
                  .Results;

            //return three results with "collect" aggregation
            //MATCH (actor:Person {name: 'Tom Hanks'})-[:ACTED_IN]->(jointMovies)<-[:ACTED_IN]-(actor2:Person {name: 'Meg Ryan'}) RETURN actor, actor2, collect(jointMovies), count(jointMovies)
            var actorsAndJointMovies = client.Cypher
                  .Match("(actor:Person {name: {actorName}})-[:ACTED_IN]->(jointMovies)<-[:ACTED_IN]-(actor2:Person {name: {actorName2}})")
                  .WithParam("actorName", "Tom Hanks")
                  .WithParam("actorName2", "Meg Ryan")
                  .Return((actor, actor2, jointMovies) => new {
                      ActorPerson = actor.As<Person>(),
                      ActorPerson2 = actor2.As<Person>(),
                      JointMovies = jointMovies.CollectAs<Movie>(),
                      JointMoviesCount = jointMovies.Count()
                  })
                  .Results;

            //create a new movie for Tom Hanks  
            var newTomHanksMovie = new Movie
            {
                Title = "Really New",
                TagLine = "Should be interesting...",
                Released = 2020
            };

            //CREATE (nm:Movie {title:'Really New', released:2020, tagline:'Should be interesting...'})
            client.Cypher
                .Create("(nm:Movie {newMovie})")
                .WithParam("newMovie", newTomHanksMovie)
                .ExecuteWithoutResults();

            //create relationship ACTED_IN for Tom Hanks in that movie
            //MATCH (tom:Person {name: 'Tom Hanks'}), (nm:Movie {title:'Really New' }) CREATE (tom)-[:ACTED_IN]->(nm)
            client.Cypher
                .Match("(tom:Person {name: {actorName}})", "(nm:Movie {title: {movieTitle}})")
                .WithParam("actorName", "Tom Hanks")
                .WithParam("movieTitle", "Really New")
                .Create("(tom)-[:ACTED_IN]->(nm)")
                .ExecuteWithoutResults();

            //delete relationship
            //MATCH (tom:Person {name: 'Tom Hanks'})-[r:ACTED_IN]->(nm:Movie {title: 'Really New'}) DELETE r
            client.Cypher
              .Match("(tom:Person {name: {actorName}})-[r:ACTED_IN]->(nm:Movie {title: {movieTitle}})")
              .WithParam("actorName", "Tom Hanks")
              .WithParam("movieTitle", "Really New")
              .Delete("r")
              .ExecuteWithoutResults();

            //delete new movie
            //MATCH (nm:Movie {title: 'Really New'}) DELETE nm
            client.Cypher
              .Match("(nm:Movie {title: {movieTitle}})")
              .WithParam("movieTitle", "Really New")
              .Delete("nm")
              .ExecuteWithoutResults();

        }
  
    }
}
