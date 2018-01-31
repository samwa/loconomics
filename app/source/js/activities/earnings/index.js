/**
 * Earnings activity that enables professionals to view 
 * a summary of their total earnings in and outside of 
 * Loconomics.
 *
 * @module activities/_examples/a-basic-activity
 *
 */

import '../../kocomponents/earnings/summary';
import * as activities from '../index';
import Activity from '../../components/Activity';
import UserType from '../../enums/UserType';
import ko from 'knockout';
import template from './template.html';

const ROUTE_NAME = 'earnings';

export default class EarningsActivity extends Activity {

    static get template() { return template; }

    constructor($activity, app) {
        /* eslint max-statements:"off" */

        super($activity, app);

        this.accessLevel = UserType.serviceProfessional;
        this.navBar = Activity.createSectionNavBar(null);
        this.navBar.rightAction(null);


        /**
         * Earnings summary returned given query parameters.
         * @member {KnockoutObservable<boolean>}
         */
        this.showFilter = ko.observable(false);

        this.showFilters = function() {
            if (this.showFilter() == false) {
                this.showFilter(true);
            }
            else {
                this.showFilter(false);
            }
        }; 


        this.title = 'Earnings';
    }
}

activities.register(ROUTE_NAME, EarningsActivity);
