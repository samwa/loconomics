/**
 * Displays a badge assigned to an user, or in OpenBadges terminology, an 'assertion'.
 *
 * In detail:
 * Displays badges users have earned. Loconomics badges are currently issued through badgr.io
 * How it works:
 * The 'src' parameter is a URL that includes user-specific information
 * about a user's single badge including:
 * - 'image' of the badge
 * - 'evidence' (optional)
 * - 'narrative'
 * - the 'badge' URL pointing to the general info about the badge
 * The 'badge' contains the following information:
 * - 'name' of the badge
 * - 'narrative' of the badge which is a description
 *
 * To populate this information and display in the component:
 * - we fetch the 'assertion' json object
 * - we amend the 'badge' url to point it to the json url
 * - we fetch the amended 'badge' json object
 * - we populate the properties
 *
 * @module kocomponents/badge-viewer
 */

import '../../utilities/icon-dec.js';
import * as badges from '../../../data/badges';
import Komponent from '../../helpers/KnockoutComponent';
import getObservable from '../../../utils/getObservable';
import ko from 'knockout';
import style from './style.styl';
import template from './template.html';

const TAG_NAME = 'badge-viewer';

/**
 * Component
 */
export default class BadgeViewer extends Komponent {

    static get style() { return style; }

    static get BadgeViewer() { return BadgeViewer; }

    static get template() { return template; }

    static get cssClass() { return 'BadgeViewer'; }

    /**
     * @param {object} params
     * @param {(string|KnockoutObservable<string>)} params.assertionURL
     * @param {(string|KnockoutObservable<string>)} [params.editURL]
     * @param {(string|KnockoutObservable<string>)} [params.viewURL]
     */
    constructor(params) {
        super();

        /**
         * The Badgr URL for the assertion (a badge issued to a person).
         * @member {string}
         */
        this.assertionURL = ko.unwrap(params.assertionURL);

        /**
         * Optional URL to enable a button/link to where to edit the badge assigned to an user
         * @member {KnockoutObservable<string>}
         */
        this.editURL = getObservable(params.editURL);

        /**
         * Optional URL to enable a button/link to where to view more details about the badge assigned to an user
         * @member {KnockoutObservable<string>}
         */
        this.detailsURL = getObservable(params.detailsURL);

        /**
         * The mode the viewer is to be shown.
         * 'card':
         * 'fullDetails':
         * @member {KnockoutObservable<string>}
         */
        this.viewMode = getObservable(params.viewMode || 'card');

         /**
         * Holds the id of the badge assertion.
         * @member {KnockoutObservable<integer>}
         */
        this.id = ko.observable();

        /**
         * Holds the image of the badge assertion.
         * @member {KnockoutObservable<string>}
         */
        this.image = ko.observable();

        /**
         * Holds the image of the badge assertion.
         * @member {KnockoutObservable<string>}
         */
        this.narrative = ko.observable();

        /**
         * Holds the image of the badge assertion.
         * @member {KnockoutObservable<string>}
         */
        this.evidence = ko.observable();

        /**
         * Holds the image of the badge assertion.
         * @member {KnockoutObservable<string>}
         */
        this.badgeName = ko.observable();

        /**
         * Holds the image of the badge assertion.
         * @member {KnockoutObservable<string>}
         */
        this.badgeDescription = ko.observable();

        this.__setupDataOperations();
    }

    /**
     * Make requests for data
     * @private
     */
    __setupDataOperations() {

        /**
         * Populate the assertion info plus badge general info
         * for the requested assertion URL.
         */
        badges.getAssertion(this.assertionURL)
        .then((assertion) => {
            this.id(assertion.id);
            this.image(assertion.image);
            this.narrative(assertion.narrative);
            this.evidence(assertion.evidence);
            this.badgeName(assertion.badge.name);
            this.badgeDescription(assertion.badge.description);
        });
    }
}

ko.components.register(TAG_NAME, BadgeViewer);
