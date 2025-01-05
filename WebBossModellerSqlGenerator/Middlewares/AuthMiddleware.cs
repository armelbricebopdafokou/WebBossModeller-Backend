
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Text;
using System;
using JsonWebToken;

namespace WebBossModellerSqlGenerator.Middlewares
{
    public class AuthMiddleware : IMiddleware
    {
        private readonly ILogger<AuthMiddleware> _logger;
        public AuthMiddleware(ILogger<AuthMiddleware> logger)
        {
            _logger = logger;
        }
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            DateTime startTime = DateTime.Now;
            //the code will execute when is requesting
            var jwtSecret = "";
            var authHeader = context.Request.Headers.Authorization;
            if (authHeader.Count()<=0)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized access - authentication required.");
                return; // Stop further processing in the pipeline
                
            }
            var token = authHeader[0].Split(" ");
            try
            {
                
            //    const decoded = jwt.verify(token, jwtSecret) as TokenPayload;

            //    req.user = {
            //    _id: decoded.id,
            //email: decoded.email,
            //createdAt: new Date(decoded.iat * 1000),
            //updatedAt: new Date(decoded.exp * 1000),
            //name: decoded.name,
            //password: "",
            //};
            //    return next();
            }
            catch (Exception ex)
            {
                //console.error(error);
                //return next(new ApiError(401, "Invalid token"));
            }

            await next(context);

            //the code will execute when is reponding
            DateTime endTime = DateTime.Now;
            var responseTime = endTime - startTime;

            _logger.LogInformation($"Response Time : {responseTime.TotalMilliseconds}");

        }
    }
}
