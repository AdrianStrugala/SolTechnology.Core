using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace SolTechnology.Core.CQRS.Errors;

public class AggregateError : Error
{
  private readonly IEnumerable<Error> _innerExceptions;

  
  /// <summary>Initializes a new instance of the <see cref="T:System.AggregateException" /> class with references to the inner exceptions that are the cause of this exception.</summary>
  /// <param name="innerExceptions">The exceptions that are the cause of the current exception.</param>
  /// <exception cref="T:System.ArgumentNullException">The <paramref name="innerExceptions" /> argument is null.</exception>
  /// <exception cref="T:System.ArgumentException">An element of <paramref name="innerExceptions" /> is null.</exception>
  public AggregateError(IEnumerable<Error> innerExceptions)
    : this("One or more errors occurred.", innerExceptions)
  {
  }
  
  
  /// <summary>Initializes a new instance of the <see cref="T:System.AggregateException" /> class with a specified error message and references to the inner exceptions that are the cause of this exception.</summary>
  /// <param name="message">The error message that explains the reason for the exception.</param>
  /// <param name="innerExceptions">The exceptions that are the cause of the current exception.</param>
  /// <exception cref="T:System.ArgumentNullException">The <paramref name="innerExceptions" /> argument is null.</exception>
  /// <exception cref="T:System.ArgumentException">An element of <paramref name="innerExceptions" /> is null.</exception>
  public AggregateError(string message, IEnumerable<Error> innerExceptions)
  {
    _innerExceptions = new List<Error>(innerExceptions ?? throw new ArgumentNullException(nameof(innerExceptions)))
      .ToArray();
  }


  /// <summary>Gets a read-only collection of the <see cref="T:System.Exception" /> instances that caused the current exception.</summary>
  /// <returns>A read-only collection of the <see cref="T:System.Exception" /> instances that caused the current exception.</returns>
  public ReadOnlyCollection<Error> InnerErrors => new((IList<Error>)_innerExceptions);


  /// <summary>Gets a message that describes the exception.</summary>
  /// <returns>The message that describes the exception.</returns>
  public override string Message
  {
    get
    {
      if (!_innerExceptions.Any())
        return base.Message;
      StringBuilder valueStringBuilder = new StringBuilder();
      valueStringBuilder.Append(base.Message);
      valueStringBuilder.Append(' ');
      for (int index = 0; index < this._innerExceptions.Count(); ++index)
      {
        valueStringBuilder.Append('(');
        valueStringBuilder.Append(this._innerExceptions.ElementAt(index).Message);
        valueStringBuilder.Append(") ");
      }

      --valueStringBuilder.Length;
      return valueStringBuilder.ToString();
    }
    set => throw new NotImplementedException();
  }

  /// <summary>Creates and returns a string representation of the current <see cref="T:System.AggregateException" />.</summary>
  /// <returns>A string representation of the current exception.</returns>
  public override string ToString()
  {
    StringBuilder stringBuilder = new StringBuilder();
    stringBuilder.Append(base.ToString());
    for (int index = 0; index < _innerExceptions.Count(); ++index)
    {
      stringBuilder.Append("\n ---> ");
      stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "(Inner Exception #{0}) ", index
      );
      stringBuilder.Append(_innerExceptions.ElementAt(index));
      stringBuilder.Append("<---");
      stringBuilder.AppendLine();
    }

    return stringBuilder.ToString();
  }

  internal int InnerExceptionCount => _innerExceptions.Count();
}
