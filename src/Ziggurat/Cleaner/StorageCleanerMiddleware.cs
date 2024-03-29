﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Ziggurat.Idempotency;

namespace Ziggurat.Cleaner
{
    public class StorageCleanerMiddleware
    {
        private readonly RequestDelegate _next;
        private ILogger<StorageCleanerMiddleware> _logger;
        private IStorage _storage;
        private readonly int _deleteOltherThanDays;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="next">the Middleware next default Microsoft implemntation</param>
        /// <param name="deleteOltherThanDays">The number of days max history allowed so that cleans older than those</param>
        public StorageCleanerMiddleware(RequestDelegate next, int deleteOltherThanDays)
        {
            _deleteOltherThanDays = deleteOltherThanDays;
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            ILogger<StorageCleanerMiddleware> logger,
            IStorage storage)
        {
            _logger = logger;
            _storage = storage;

            //this avoids muiltiple calls on middleeware since the controller "/"
            //and subsquent search for favicon locations is passed automatically by .net core framework
            //It can be catched with below log if needeed
            //_logger.LogWarning("CONTEXT: {path}", context.Request.Path.Value);
            if (context != null && context.Request != null && !context.Request.Path.Value.Contains("favicon"))
            {
                _logger.LogInformation("Set to clean older than {deleteOltherThanDays} days.", _deleteOltherThanDays);
                await DeleteMessageHistory(_deleteOltherThanDays);
            }

            await _next(context);
        }

        protected async Task DeleteMessageHistory(int deleteOltherThanDays)
        {
            try
            {
                var deleteCount = await _storage.DeleteMessagesHistoryOltherThanAsync(deleteOltherThanDays);
                _logger.LogInformation("Deleted {deleteCount} messages older than {olderThanDays} days.", deleteCount, deleteOltherThanDays);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete messages history failed with error: {message}", ex.InnerException.Message ?? ex.Message);
            }
        }
    }
}
