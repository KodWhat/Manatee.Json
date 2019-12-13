using System;
using System.Linq.Expressions;

namespace Manatee.Json.Path.Expressions.Translation
{
	internal class AddExpressionTranslator : IExpressionTranslator
	{
		public ExpressionTreeNode<T> Translate<T>(Expression body)
		{
			var add = body as BinaryExpression;
			if (add == null)
				throw new InvalidOperationException();
			return new AddExpression<T>(ExpressionTranslator.TranslateNode<T>(add.Left),
			                            ExpressionTranslator.TranslateNode<T>(add.Right));
		}
	}
}