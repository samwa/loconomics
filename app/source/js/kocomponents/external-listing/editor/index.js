/**
 * Allows a professional to add or edit information about a
 * listing on an external platform.
 *
 * @module kocomponents/external-listing/editor
 */

import '../../utilities/icon-dec.js';
import { ActionForValue } from '../../job-title-autocomplete';
import Komponent from '../../helpers/KnockoutComponent';
import getObservable from '../../../utils/getObservable';
import ko from 'knockout';
import { item as platformsItem } from '../../../data/platforms';
import { show as showConfirm } from '../../../modals/confirm';
import { show as showError } from '../../../modals/error';
import template from './template.html';
import { item as userExternalListingItem } from '../../../data/userExternalListings';

const TAG_NAME = 'external-listing-editor';

/**
 * Component
 */
export default class ExternalListingEditor extends Komponent {

    static get template() { return template; }

    /**
     * @param {object} params
     * @param {(number|KnockoutObservable<number>)}
     * [params.platformID]
     * @param {(number|KnockoutObservable<number>)}
     * [params.externalListingID]
     */

    constructor(params) {
        super();
        /// Form data
        /**
         * Holds the ID for the external listing being
         * edited.
         * @member {KnockoutObservable<number>}
         */
        this.externalListingID = getObservable(params.externalListingID);

        /**
         * Holds the ID for the platform of the external
         * listing being created.
         * @member {KnockoutObservable<number>}
         */
        this.externalPlatformID = getObservable(params.platformID);

        /**
         * Holds the ID for the platform of the external
         * listing being created.
         * @member {KnockoutObservable<number>}
         */
        this.platformName = getObservable(params.platformName);

        /**
         * Holds the data of the external listing.
         * @member {KnockoutObservable<object>}
         */
        this.externalListing = ko.observable(null);

        /**
         * Holds the basic information about the external
         * platform of the listing being added.
         * @member {KnockoutObservable<object>}
         */
        this.externalPlatformInfo = ko.observable();

        /**
         * Holds a list of Job Title selected by the user using the autocomplete.
         * The list will be populated too with saved data when a listing is loaded.
         * @member {KnockoutObservableArray<Object>}
         */
        this.selectedJobTitles = ko.observableArray([]);

        /**
         * Callback executed after a succesfully 'save' task, providing
         * the updated data.
         * When there is no one, the data returned by the server is used to
         * update currently displayed data.
         * @member {Function<rest/UserExternalListing>}
         */
        this.onSaved = ko.unwrap(params.onSaved);

        /**
         * State flag for the 'save' task
         * @member {KnockoutObservable<boolean>}
         */
        this.isSaving = ko.observable(false);

        /**
         * Dynamic label text for the 'save' button
         * @member {KnockoutComputed<string>}
         */
        this.saveButtonLabel = ko.pureComputed(() => {
            const text = this.isSaving() ? 'Saving...' : 'Save';
            return text;
        });

        /**
         * Callback executed after a succesfully 'delete' task.
         * When there is no one, the current data and ID are cleared.
         * @member {Function}
         */
        this.onDeleted = ko.unwrap(params.onDeleted);

        /**
         * State flag for the 'delete' task
         * @member {KnockoutObservable<boolean>}
         */
        this.isDeleting = ko.observable(false);

        /**
         * Dynamic label text for the 'delete' button
         * @member {KnockoutComputed<string>}
         */
        this.deleteButtonLabel = ko.pureComputed(() => {
            const text = this.isDeleting() ? 'Deleting...' : 'Delete';
            return text;
        });

        /**
         * State flag. Whether a task is in progress, then user editing should
         * not be allowed.
         * @member {KnockoutComputed<boolean>}
         */
        this.isWorking = ko.pureComputed(() => this.isSaving() || this.isDeleting());

        /**
         * Loads the information about the platform, returning and placing it
         * at the member externalPlatformInfo and updating member platformName.
         * @param {number} id
         * @private
         */
        const loadPlatformInfo = (id) => {
            let promise;
            if (id) {
                promise = platformsItem(id).onceLoaded();
            }
            else {
                promise = Promise.resolve({});
            }
            return promise.then((data) => {
                this.externalPlatformInfo(data);
                this.platformName(data.name);
                return data;
            });
        };

        /**
         * Displays the error returned while loading the data
         * @param {Error} err
         */
        const loadingError = function(error) {
            showError({
                title: 'There was an error loading the listing',
                error
            });
        };

        /**
         * React to changes to the instance used as external listing:
         * - populate list of editable/selected Job Title IDs
         */
        this.subscribeTo(this.externalListing, (data) => {
            const list = Object.keys(data.jobTitles)
            .map((id) => ({
                id,
                name: data.jobTitles[id]
            }));
            this.selectedJobTitles(list);
        });

        /**
         * When the given listing ID changes, load the data or set-up a new
         * entry for a platformID specified.
         */
        this.observeChanges(() => {
            const id = this.externalListingID();
            if (id) {
                userExternalListingItem(id)
                .onceLoaded()
                .then((data) => {
                    this.externalListing(data);
                    return loadPlatformInfo(data.platformID);
                })
                .catch(loadingError);
            }
            else {
                loadPlatformInfo(this.externalPlatformID())
                .then(() => {
                    this.externalListing({
                        externalListingID: 0,
                        platformID: this.externalPlatformID(),
                        title: 'My ' + this.platformName() + ' listing',
                        jobTitles: {},
                        notes: '',
                        modifiedDate: false
                    });
                    // Empty list of selected job titles
                    this.selectedJobTitles([]);
                })
                .catch(loadingError);
            }
        });
    }

    /**
     * Save current form data
     */
    save() {
        // Prevent twice execution
        if (this.isSaving()) return;
        // Initial sync code wrappein a promise, so in case of error we
        // catch it later.
        return new Promise((resolve, reject) => {
            this.isSaving(true);
            // Copy data to send
            const data = Object.assign({}, this.externalListing());
            // Replace the object of job titles with the list of selected IDs
            data.jobTitles = this.selectedJobTitles().map((jt) => jt.id);
            // Sent data
            const item = userExternalListingItem(this.externalListingID());
            item.save(data).then(resolve, reject);
        })
        // Update with server data
        .then((serverData) => {
            this.isSaving(false);
            if (this.onSaved) {
                this.onSaved(serverData);
            }
            else {
                this.externalListing(serverData);
                this.externalListingID(serverData.userExternalListingID);
            }
        })
        .catch((error) => {
            this.isSaving(false);
            showError({
                title: 'There was an error saving your changes',
                error
            });
        });
    }

    onSelectJobTitle(textValue, data) {
        if (!data) return;
        if (data.jobTitleID) {
            const id = data.jobTitleID();
            // Add to list of selected ones
            this.selectedJobTitles.push({
                id,
                name: textValue
            });
        }
        return {
            value: ActionForValue.clear
        };
    }

    unselectJobTitle(jobTitleItem) {
        this.selectedJobTitles.remove(jobTitleItem);
    }

    /**
     * Request to delete a listing, with user confirmation.
     * It triggers 'onDeleted' param, or remove current data and ID.
     * @returns {Promise}
     */
    deleteListing() {
        // Only a deletion task at a time
        if (this.isDeleting()) return;
        // Only do something if data loaded
        const listing = this.externalListing();
        if (listing && listing.userExternalListingID) {
            return showConfirm({
                title: 'Delete listing',
                message: `Are you sure to delete '${listing.title}'?`,
                yes: 'Delete',
                no: 'Keep'
            })
            .then(() => {
                this.isDeleting(true);
                return userExternalListingItem(listing.userExternalListingID)
                .delete()
                .then(() => {
                    this.isDeleting(false);
                    if (this.onDeleted) {
                        this.onDeleted();
                    }
                    else {
                        this.externalListing(null);
                        this.externalListingID(null);
                    }
                });
            })
            .catch((error) => {
                // Only on error; error is undefined when user declined
                if (error) {
                    showError({
                        title: 'There was an error when deleting your listing',
                        error
                    });
                }
            });
        }
        else {
            return Promise.resolve();
        }
    }
}

ko.components.register(TAG_NAME, ExternalListingEditor);
