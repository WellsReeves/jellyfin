using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.ModelBinders
{
    /// <summary>
    /// Comma delimited array model binder.
    /// Returns an empty array of specified type if there is no query parameter.
    /// </summary>
    public class CommaDelimitedArrayModelBinder : IModelBinder
    {
        private readonly ILogger<CommaDelimitedArrayModelBinder> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommaDelimitedArrayModelBinder"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{CommaDelimitedArrayModelBinder}"/> interface.</param>
        public CommaDelimitedArrayModelBinder(ILogger<CommaDelimitedArrayModelBinder> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            var elementType = bindingContext.ModelType.GetElementType() ?? bindingContext.ModelType.GenericTypeArguments[0];
            var converter = TypeDescriptor.GetConverter(elementType);

            if (valueProviderResult.Length > 1)
            {
                var typedValues = GetParsedResult(valueProviderResult.Values, elementType, converter);
                bindingContext.Result = ModelBindingResult.Success(typedValues);
            }
            else
            {
                var value = valueProviderResult.FirstValue;

                if (value != null)
                {
                    var splitValues = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    var typedValues = GetParsedResult(splitValues, elementType, converter);
                    bindingContext.Result = ModelBindingResult.Success(typedValues);
                }
                else
                {
                    var emptyResult = Array.CreateInstance(elementType, 0);
                    bindingContext.Result = ModelBindingResult.Success(emptyResult);
                }
            }

            return Task.CompletedTask;
        }

        private Array GetParsedResult(IReadOnlyList<string> values, Type elementType, TypeConverter converter)
        {
            var parsedValues = new object?[values.Count];
            var convertedCount = 0;
            for (var i = 0; i < values.Count; i++)
            {
                try
                {
                    parsedValues[i] = converter.ConvertFromString(values[i].Trim());
                    convertedCount++;
                }
                catch (FormatException e)
                {
                    _logger.LogWarning(e, "Error converting value.");
                }
            }

            var typedValues = Array.CreateInstance(elementType, convertedCount);
            var typedValueIndex = 0;
            for (var i = 0; i < parsedValues.Length; i++)
            {
                if (parsedValues[i] != null)
                {
                    typedValues.SetValue(parsedValues[i], typedValueIndex);
                    typedValueIndex++;
                }
            }

            return typedValues;
        }
    }
}
