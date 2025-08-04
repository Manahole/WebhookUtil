using Cronos;
using System.Reactive.Linq;
using System.Reactive;

namespace WebhookUtil
{
    public static class CronObservable
    {
        /// <summary>
        /// Crée un IObservable qui émet selon un pattern cron spécifié
        /// </summary>
        /// <param name="cronExpression">Expression cron (ex: "0 */5 * * * *" pour toutes les 5 minutes)</param>
        /// <param name="timeZone">Fuseau horaire (optionnel, par défaut UTC)</param>
        /// <param name="cancellationToken">Token d'annulation (optionnel)</param>
        /// <returns>IObservable qui émet Unit aux moments définis par l'expression cron</returns>
        public static IObservable<Unit> CreateCronObservable(
            string cronExpression,
            TimeZoneInfo timeZone = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(cronExpression))
                throw new ArgumentException("L'expression cron ne peut pas être vide", nameof(cronExpression));

            // Parse l'expression cron
            CronExpression cron;
            try
            {
                cron = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);
            }
            catch (CronFormatException ex)
            {
                throw new ArgumentException($"Expression cron invalide: {ex.Message}", nameof(cronExpression));
            }

            timeZone ??= TimeZoneInfo.Utc;

            return Observable.Create<Unit>(observer =>
            {
                var subscription = new CancellationTokenSource();
                var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken, subscription.Token).Token;

                // Démarrer la tâche de scheduling en arrière-plan
                _ = ScheduleCronTask(cron, timeZone, observer, combinedToken);

                return subscription;
            });
        }

        private static async Task ScheduleCronTask(
            CronExpression cron,
            TimeZoneInfo timeZone,
            IObserver<Unit> observer,
            CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone);
                    var nextOccurrence = cron.GetNextOccurrence(now, timeZone);

                    if (!nextOccurrence.HasValue)
                    {
                        // Plus d'occurrences futures, compléter l'observable
                        observer.OnCompleted();
                        return;
                    }

                    var delay = nextOccurrence.Value - now;

                    // Attendre jusqu'à la prochaine occurrence
                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay, cancellationToken);
                    }

                    // Vérifier si on n'a pas été annulé pendant l'attente
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        observer.OnNext(Unit.Default);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Annulation normale, ne pas émettre d'erreur
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        }

        /// <summary>
        /// Version simplifiée qui utilise le fuseau horaire local
        /// </summary>
        /// <param name="cronExpression">Expression cron</param>
        /// <returns>IObservable qui émet selon le pattern cron</returns>
        public static IObservable<Unit> CreateCronObservable(string cronExpression)
        {
            return CreateCronObservable(cronExpression, TimeZoneInfo.Local);
        }
    }
}
