using System.Text.Json;
using System.Net;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using RSFrancisco.AWS.Lambda.Models;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace RSFrancisco.AWS.Lambda;

public class Function
{
    private readonly DynamoDBContext _dynamoDBContext;

    public Function()
    {
        _dynamoDBContext = new DynamoDBContext(new AmazonDynamoDBClient());
    }


    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="input"></param>
    /// <param name="context"></param>
    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        return request.RequestContext.Http.Method.ToUpper() 
            switch
            {
                "GET" => await GetRequestHandler(request),
                "POST" => await PostRequestHandler(request),
                "DELETE" => await DeleteRequestHandler(request)
            };
    }





    private async Task<APIGatewayHttpApiV2ProxyResponse> DeleteRequestHandler(APIGatewayHttpApiV2ProxyRequest request)
    {
        request.PathParameters.TryGetValue("userId", out var userIdString);

        if (Guid.TryParse(userIdString, out var userId))
        {
            await _dynamoDBContext.DeleteAsync<User>(userId);
            return ResponseMessage.Success(true);
        }

        return ResponseMessage.Failure("Resource not found", HttpStatusCode.NotFound);
    }
    private async Task<APIGatewayHttpApiV2ProxyResponse> PostRequestHandler(APIGatewayHttpApiV2ProxyRequest request)
    {
        User user = JsonSerializer.Deserialize<User>(request.Body);
        if (user is null)
            return ResponseMessage.Failure("Object is required", HttpStatusCode.BadRequest);

        await _dynamoDBContext.SaveAsync(user);

        return ResponseMessage.Success(true, HttpStatusCode.Created);
    }
    private async Task<APIGatewayHttpApiV2ProxyResponse> GetRequestHandler(APIGatewayHttpApiV2ProxyRequest request)
    {
        request.PathParameters.TryGetValue("userId", out var userIdString);

        if (Guid.TryParse(userIdString, out var userId))
        {
            User user = await _dynamoDBContext.LoadAsync<User>(userId);

            if (user is not null)
            {
                return ResponseMessage.Success(user);
            }
        }

        return ResponseMessage.Failure("Resource not found", HttpStatusCode.NotFound);
    }
}

public static class ResponseMessage
{
    public static APIGatewayHttpApiV2ProxyResponse Success(Object obj, HttpStatusCode statusCode = HttpStatusCode.OK) => new APIGatewayHttpApiV2ProxyResponse
    {
        Body = JsonSerializer.Serialize(obj),
        StatusCode = (int)statusCode
    };

    public static APIGatewayHttpApiV2ProxyResponse Failure(string message, HttpStatusCode statusCode) => new APIGatewayHttpApiV2ProxyResponse
    {
        Body = JsonSerializer.Serialize(new { message = message }),
        StatusCode = (int)statusCode
    };
}