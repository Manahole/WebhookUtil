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

        /// <summary>
        /// Version qui garde le même type - applique conditionnellement l'une des deux transformations
        /// </summary>
        /// <typeparam name="T">Type des éléments de l'observable</typeparam>
        /// <param name="source">Observable source</param>
        /// <param name="condition">Fonction qui retourne true ou false</param>
        /// <param name="trueTransform">Transformation à appliquer si la condition est vraie</param>
        /// <param name="falseTransform">Transformation à appliquer si la condition est fausse</param>
        /// <returns>Observable transformé selon la condition</returns>
        public static IObservable<T> SwitchIf<T>(
            this IObservable<T> source,
            Func<bool> condition,
            Func<IObservable<T>, IObservable<T>> trueTransform,
            Func<IObservable<T>, IObservable<T>> falseTransform)
        {
            return source.SwitchIf<T, T>(condition, trueTransform, falseTransform);
        }

        /// <summary>
        /// Version avec cas par défaut
        /// </summary>
        /// <typeparam name="TSource">Type des éléments de l'observable source</typeparam>
        /// <typeparam name="TResult">Type des éléments de l'observable résultant</typeparam>
        /// <param name="source">Observable source</param>
        /// <param name="defaultTransform">Transformation par défaut si aucune condition n'est vraie</param>
        /// <param name="cases">Tableau de cas conditionnels à tester dans l'ordre</param>
        /// <returns>Observable transformé selon la première condition vraie ou la transformation par défaut</returns>
        public static IObservable<TResult> SwitchIf<TSource, TResult>(
            this IObservable<TSource> source,
            Func<IObservable<TSource>, IObservable<TResult>> defaultTransform,
            params ConditionalCase<TSource, TResult>[] cases)
        {
            return source.SelectMany(item =>
            {
                foreach (var conditionalCase in cases)
                {
                    if (conditionalCase.Condition(item))
                    {
                        return conditionalCase.Transform(Observable.Return(item));
                    }
                }

                // Cas par défaut
                return defaultTransform(Observable.Return(item));
            });
        }

        /// <summary>
        /// Méthode helper pour créer un cas conditionnel
        /// </summary>
        public static ConditionalCase<TSource, TResult> Case<TSource, TResult>(
            Func<TSource, bool> condition,
            Func<IObservable<TSource>, IObservable<TResult>> transform)
        {
            return new ConditionalCase<TSource, TResult>(condition, transform);
        }

        /// <summary>
        /// Applique conditionnellement l'une des transformations selon les conditions testées sur chaque élément
        /// </summary>
        /// <typeparam name="TSource">Type des éléments de l'observable source</typeparam>
        /// <typeparam name="TResult">Type des éléments de l'observable résultant</typeparam>
        /// <param name="source">Observable source</param>
        /// <param name="cases">Tableau de cas conditionnels à tester dans l'ordre</param>
        /// <returns>Observable transformé selon la première condition vraie pour chaque élément</returns>
        public static IObservable<TResult> SwitchIf<TSource, TResult>(
            this IObservable<TSource> source,
            params ConditionalCase<TSource, TResult>[] cases)
        {
            return source.SelectMany(item =>
            {
                foreach (var conditionalCase in cases)
                {
                    if (conditionalCase.Condition(item))
                    {
                        return conditionalCase.Transform(Observable.Return(item));
                    }
                }

                // Si aucune condition n'est vraie, lever une exception
                throw new InvalidOperationException($"Aucune condition n'a été satisfaite pour l'élément {item} et aucun cas par défaut n'a été fourni.");
            });
        }

        /// <summary>
        /// Accumule les éléments d'un Observable jusqu'à ce que le CronObservable émette
        /// </summary>
        /// <typeparam name="T">Type des éléments de l'Observable source</typeparam>
        /// <param name="source">Observable source dont on veut accumuler les éléments</param>
        /// <param name="cronExpression">Expression cron qui définit quand vider le buffer</param>
        /// <param name="timeZone">Fuseau horaire (optionnel, par défaut UTC)</param>
        /// <param name="cancellationToken">Token d'annulation (optionnel)</param>
        /// <returns>Observable de listes contenant les éléments accumulés</returns>
        public static IObservable<IList<T>> BufferByCron<T>(
            this IObservable<T> source,
            string cronExpression,
            TimeZoneInfo timeZone = null,
            CancellationToken cancellationToken = default)
        {
            var cronTrigger = CronObservable.CreateCronObservable(cronExpression, timeZone, cancellationToken);
            return source.Buffer(cronTrigger);
        }
    }
    /// <summary>
    /// Classe pour représenter un cas conditionnel
    /// </summary>
    /// <typeparam name="TSource">Type des éléments de l'observable source</typeparam>
    /// <typeparam name="TResult">Type des éléments de l'observable résultant</typeparam>
    public class ConditionalCase<TSource, TResult>
    {
        public Func<TSource, bool> Condition { get; set; }
        public Func<IObservable<TSource>, IObservable<TResult>> Transform { get; set; }

        public ConditionalCase(Func<TSource, bool> condition, Func<IObservable<TSource>, IObservable<TResult>> transform)
        {
            Condition = condition;
            Transform = transform;
        }
    }
}
