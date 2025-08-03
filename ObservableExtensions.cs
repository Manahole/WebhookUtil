using System.Reactive.Linq;

namespace WebhookUtil
{
    public static class ObservableExtensions
    {
        /// <summary>
        /// Applique conditionnellement l'une des deux transformations sur l'observable source
        /// </summary>
        /// <typeparam name="TSource">Type des éléments de l'observable source</typeparam>
        /// <typeparam name="TResult">Type des éléments de l'observable résultant</typeparam>
        /// <param name="source">Observable source</param>
        /// <param name="condition">Fonction qui retourne true ou false</param>
        /// <param name="trueTransform">Transformation à appliquer si la condition est vraie</param>
        /// <param name="falseTransform">Transformation à appliquer si la condition est fausse</param>
        /// <returns>Observable transformé selon la condition</returns>
        public static IObservable<TResult> SwitchIf<TSource, TResult>(
            this IObservable<TSource> source,
            Func<bool> condition,
            Func<IObservable<TSource>, IObservable<TResult>> trueTransform,
            Func<IObservable<TSource>, IObservable<TResult>> falseTransform)
        {
            return Observable.Defer(() =>
                condition() ? trueTransform(source) : falseTransform(source));
        }
    }
}
