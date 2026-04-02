using Pillaro.Dataverse.PluginFramework.Tasks.Validation.Validators.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Pillaro.Dataverse.PluginFramework.Tasks.Validation;

internal interface IValidatorProcessor
{
    void AddValidatorHandler(IValidator validatorHandler);

    bool IsValid();

    IEnumerable<string> GetMessages();
}

internal class ValidatorProcessor : IValidatorProcessor
{
    private readonly TaskContext _taskContext;
    private readonly List<IValidator> _validatorHandlers = [];
    private readonly List<string> _messages = [];
    private bool _isValid = true;

    public ValidatorProcessor(TaskContext taskContext)
    {
        _taskContext = taskContext;
    }

    public void AddValidatorHandler(IValidator validatorHandler)
    {
        _validatorHandlers.Add(validatorHandler);
    }

    public bool IsValid()
    {
        _messages.Clear();
        foreach (var validatorHandler in _validatorHandlers.Where(x => x is IBasicValidator || x is IPredicateValidator))
        {
            if (validatorHandler is IPredicateValidator predicateValidator && !predicateValidator.IsPredicateValid(_taskContext))
            {
                //skip validation, if predicate is not valid
                _messages.Add($"{predicateValidator.GetName} skipped, reason: {predicateValidator.GetPredicateMessage()}");
                continue;
            }

            var valid = validatorHandler.Validate(_taskContext);
            _isValid = _isValid && valid;
            _messages.Add(!valid ? validatorHandler.GetMessage() : $"{validatorHandler.GetName}: OK_");
        }

        if (!_isValid)
            return false;

        // run only if all basic validations are true
        foreach (var validatorHandler in _validatorHandlers.Where(x => x is IBreakValidator))
        {
            var valid = validatorHandler.Validate(_taskContext);
            _isValid = _isValid && valid;
            if (!valid)
            {
                _messages.Add(validatorHandler.GetMessage());
                return _isValid;
            }
            else
                _messages.Add($"{validatorHandler.GetName}: OK_");
        }

        return _isValid;
    }

    public IEnumerable<string> GetMessages()
    {
        return _messages;
    }
}