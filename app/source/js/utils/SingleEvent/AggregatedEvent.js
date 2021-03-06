/**
 * The AggregatedEvent class allows to group several SingleEvents in once
 * for subscription. Emitting directly from this new event is not allowed,
 * since it automatically replays anything emitted from the other events
 * without identify who did it.
 *
 * Even at first shot may look like going against the SingleEvent pattern,
 * really every event allowed still needs be defined individually and
 * this event doesn't let to emit arbitrary event types.
 * The idea is to being able to subscribe just once and
 * get notified of all events involved, that are related in some way, see
 * the examples below.
 * @module utils/SingleEvent/AggregatedEvent
 * @example
 * ```
 * const onLoadError = new SingleEvent();
 * const onSaveError = new SingleEvent();
 * const onError = new AggregatedEvent([onLoadError, onSaveError]);
 * onError.subscribe((err) => console.error('Something went wrong', err));
 * ```
 */
import SingleEvent from './index';

export default class AggregatedEvent extends SingleEvent {
    /**
     * Class constructor
     * @param {Array<SingleEvent>} events List of events included in this
     * aggregation, replayed through it.
     * @param {Object} context The context used when replaying the events.
     */
    constructor(events, context) {
        super(context);
        // We get a reference to the 'emit' method in the closure...
        const emit = this.emit.bind(this);
        // ...so we can remove it as a method, then not allowing this new
        // event to emit explicitly (only replayes other events)
        delete this.emit;

        /**
         * Keep an array of subscriptions to source events (one per event).
         * Is null when there are no subscriptions to the aggregated event.
         * @private {Array<SingleEvent/Subscription>}
         */
        let subscriptionsToSourceEvents = null;

        /**
         * Original subscribe method, will be replaced but reused inside
         * @private {Function}
         */
        const subscribe = this.subscribe;
        /**
         * Subscribe to event notifications, happening when the source events
         * emit.
         * Check SingleEvent..subscribe documentation for detailed parameters
         * @param {Array<any>} args
         */
        this.subscribe = (...args) => {
            var result = subscribe.apply(this, args);
            if (this.count === 1) {
                // First time, need to suscribe to source events
                subscriptionsToSourceEvents = events
                .map((event) => event.subscribe((...params) => {
                        emit(...params);
                    })
                );
            }
            return result;
        };

        /**
         * Original unsubscribe method, will be replaced but reused inside
         * @private {Function}
         */
        const unsubscribe = this.unsubscribe;
        /**
         * Remove a subscription to the event; it will remove subscription to
         * source events if no active subscriptions to the aggregated.
         * Check SingleEvent..unsubscribe documentation for detailed parameters
         * @param {Array<any>} args
         */
        this.unsubscribe = (...args) => {
            var result = unsubscribe.apply(this, args);
            if (this.count === 0 && subscriptionsToSourceEvents) {
                // If no remaining subscriptions, no need to keep subscribed
                // to source events
                subscriptionsToSourceEvents.forEach((subs) => subs.dispose());
                subscriptionsToSourceEvents = null;
            }
            return result;
        };
    }
}
