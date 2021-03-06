/**
 * Used for adding, editing, and copying earnings entries.
 * @module kocomponents/earnings/editor
 *
 */
import '../../client/editor';
import '../../client/list';
import '../../utilities/icon-dec';
import '../../input/duration';
import '../../../utils/autofocusBindingHandler';
import '../../../utils/pressEnterBindingHandler';
import '../../input/date';
import * as suggestedPlatformsList from '../../../data/suggestedPlatforms';
import { ActionForValue } from '../../job-title-autocomplete';
import Komponent from '../../helpers/KnockoutComponent';
import UserEarningsEntry from '../../../models/UserEarningsEntry';
import { item as clientsDataItem } from '../../../data/clients';
import ko from 'knockout';
import { show as showConfirm } from '../../../modals/confirm';
import { show as showError } from '../../../modals/error';
import template from './template.html';
import { item as userEarningsItem } from '../../../data/userEarnings';
import { list as userExternalListingsList } from '../../../data/userExternalListings';
import { list as userListingsList } from '../../../data/userListings';

const TAG_NAME = 'earnings-editor';

/**
 * @enum {string} Supported displaying modes
 */
const EditorMode = {
    add: 'add',
    edit: 'edit',
    copy: 'copy'
};

/**
 * Component
 */
export default class EarningsEditor extends Komponent {

    static get template() { return template; }

    /**
     * Parameters allowed are 'input only' when the value given is read at constructor
     * and keeps constant internally. If is an observable, any change from outside is
     * not read.
     * @param {object} params
     * @param {KnockoutObservable<EditorMode>} [params.editorMode] Input only value setting-up the mode in use
     * @param {(number|KnockoutObservable<number>)} [params.earningsEntryID] Input only ID to be edited or copied, or zero for new.
     * @param {(number|KnockoutObservable<number>)} [params.userExternalListingID] Input only ID to preset the external listing,
     * this let's add an earning from a listing component as a shortcut.
     * @param {KnockoutObservable<integer>} [params.startAtStep] Input only value setting-up the initial step for the component.
     * @param {function} [params.onSaved] Callback to notify after save the item, with the updated data included
     * @param {function} [params.onDeleted] Callback to notify after delete the item
     */
    constructor(params) {
        super();

        /**
         * Editable earnings entry. Same instance is used all the time, just
         * updating content, simplifying working with the form and summary.
         * @member {UserEarningsEntry}
         */
        this.earningsEntry = new UserEarningsEntry({
            earningsEntryID: ko.unwrap(params.earningsEntryID) || 0,
            userExternalListingID: ko.unwrap(params.userExternalListingID)
        });

        /**
         * Keeps a timestamp of the loaded data, allowing to track when there
         * are changes.
         * @member {Date}
         */
        this.dataTimestamp = ko.observable(this.earningsEntry.model.dataTimestamp());

        /**
         * Captures from the activity which "mode" the editor
         * component is to be used.
         * @member {EditorMode}
         */
        this.editorMode = ko.observable(ko.unwrap(params.editorMode));

        /**
         * @member {KnockoutObservable<object>}
         */
        this.selectedClient = ko.observable(null);

        /**
         * Callback executed when the form is saved successfully, giving
         * a copy of the server data
         * @member {function}
         */
        this.onSaved = params.onSaved;

        /**
         * Callback executed when the entry was deleted successfully
         * @member {function}
         */
        this.onDeleted = params.onDeleted;

        /**
         * Holds a list of the user external listings, available to be selected
         * as the earnings entry listing.
         * @member {KnockoutObservableArray<rest/UserExternalListing>}
         */
        this.userExternalListings = ko.observableArray([]);

        /**
         * Holds a list of the user listings at Loconomics, available to allow
         * quick selection of job title.
         * @member {KnockoutObservableArray<rest/UserJobTitle>}
         */
        this.userListings = ko.observableArray([]);

        /**
         * Holds a list of suggested platforms, available to be selected
         * as the earnigns entry listing, but while is not a listing it will
         * create a listing for that platform automatically.
         * @member {KnockoutObservableArray<rest/Platform>}
         */
        this.suggestedPlatforms = ko.observableArray([]);

        // Starts in review mode when we are editing or copying an entry,
        // so anything else but 'add'
        this.__setupStepsManagement(params.editorMode !== EditorMode.add);

        this.__setupStatusFlags();

        /**
         * Whether there are changes not saved.
         * @member {KnockoutComputed<boolean>}
         */
        this.hasUnsavedChanges = ko.pureComputed(() => {
            var c = this.earningsEntry;
            return c && this.dataTimestamp() !== c.model.dataTimestamp();
        });

        /**
         * Label text for the 'delete' button
         * @member {KnockoutComputed<string>}
         */
        this.deleteButtonText = ko.pureComputed(() => {
            var itIs = this.isDeleting();
            return itIs ? 'Deleting..' : 'Delete entry';
        });

        /**
         * Label text for the 'save' button
         * @member {KnockoutComputed<string>}
         */
        this.saveButtonText = ko.pureComputed(() => {
            // Special case when we are copying an entry and it was not
            // changed still, so looks like a duplicated entry
            const isDuplicated = this.editorMode() === EditorMode.copy && !this.hasUnsavedChanges();
            // Text depending on state:
            const text = (
                this.isLoading() ?
                'Loading...' :
                this.isSaving() ?
                'Submitting..' :
                isDuplicated ?
                'Unchanged' :
                this.isNew() ?
                'Submit' :
                this.hasUnsavedChanges() ?
                'Submit changes' :
                // anything else:
                'Submitted'
            );
            return text;
        });

        this.__setupDataOperations();
    }

    /**
     * Define members to implement a step based interface.
     * @param {boolean} [startInReview=false] Whether the interface must start at the
     * summary step with review enabled.
     * @private
     */
    __setupStepsManagement(startInReview) {
        /**
         * Magic number for the step where is the summary.
         * @const {number}
         * @private
         */
        const SUMMARY_STEP = 0;
        /**
         * Keeps track of the current step being displayed.
         * By default is 1, first step, except when starting at review that will
         * be 0 (the summary).
         * This default is in sync with isAtReview initialization.
         * @member {KnockoutObservable<integer>}
         */
        this.currentStep = ko.observable(startInReview ? SUMMARY_STEP : 1);

        /**
         * Returns which step the user is on in the form.
         * @member {KnockoutComputed<boolean>}
         */
        this.isAtStep = function(number) {
            return ko.pureComputed(() => this.currentStep() === number);
        };

        /**
         * Whether the user has completed the steps almost once and is free
         * to jump between the summary and steps in order to do touch ups.
         * By default is false, which means the user is restricted to follow
         * the steps in order until finalize and reach the summary, when
         * this flag switchs to true.
         * Can be set.
         * By default is false, except when starting at review that will be true.
         * This default is in sync with currentStep initialization.
         * @member {KnockoutObservable<boolean>}
         */
        this.isAtReview = ko.observable(!!startInReview);

        /**
         * Takes the user to the next step in the form.
         * @member {KnockoutComputed<number>}
         */
        this.goNextStep = () => {
            this.currentStep(this.currentStep() + 1);
        };

        /**
         * Takes the user to the specified step
         * @param {number} step Step number
         */
        this.goToStep = (step) => {
            this.currentStep(step);
        };

        /**
         * Takes the user to the summary.
         * Will enable review mode too.
         */
        this.goToSummary = () => {
            this.currentStep(SUMMARY_STEP);
            // Reached the summary, all steps done so enters review mode
            this.isAtReview(true);
        };

         /**
         * Takes the user to the next step in the form.
         * @member {KnockoutComputed<string>}
         */
        this.stepButtonLabel = ko.pureComputed(() => {
            if (this.isAtReview()) {
                return 'Save';
            }
            else {
                return 'Save and Continue';
            }
        });

        /**
         * Takes the user to the next step in the form (in standard mode),
         * or to the summary (when in review mode).
         * @member {KnockoutComputed<number>}
         */
        this.saveStep = () => {
            if (this.isAtReview()) {
                this.currentStep(SUMMARY_STEP);
            }
            else {
                this.goNextStep();
            }
        };
    }

    /**
     * Define members for all the status flags needed.
     * @private
     */
    __setupStatusFlags() {
        /**
         * When a loading request it's on the works
         * @member {KnockoutObservable<boolean>}
         */
        this.isLoading = ko.observable(false);

        /**
         * When a saving request it's on the works
         * @member {KnockoutObservable<boolean>}
         */
        this.isSaving = ko.observable(false);

        /**
         * When a deletion request it's on the works
         * @member {KnockoutObservable<boolean>}
         */
        this.isDeleting = ko.observable(false);

        /**
         * When edition must be locked because of in progress
         * operations.
         * @member {KnockoutComputed<boolean>}
         */
        this.isLocked = ko.pureComputed(() => this.isSaving() || this.isLoading() || this.isDeleting());

        /**
         * Whether the item is a new record or is being edited.
         * @member {KnockoutObservable<boolean>}
         */
        this.isNew = ko.pureComputed(() => this.editorMode() !== EditorMode.edit);
    }

    /**
     * Define members, prepare subscriptions to work with the code
     * and start any initial request for data
     * @private
     */
    __setupDataOperations() {
        /**
         * We create an item manager to operate on the data for the requested ID
         * (allows to load, save, delete).
         */
        this.dataManager = userEarningsItem(this.earningsEntry.earningsEntryID());

        /**
         * Suscribe to data coming for the list and put them in our
         * userListings propery.
         */
        this.subscribeTo(userListingsList.onData, this.userListings);

        /**
         * Notify data load errors
         */
        this.subscribeTo(userListingsList.onDataError, (err) => {
            showError({
                title: 'There was an error loading your listings',
                error: err
            });
        });

        /**
         * Suscribe to data coming for the list and put them in our
         * userExternalListings propery.
         */
        this.subscribeTo(userExternalListingsList.onData, this.userExternalListings);

        /**
         * Notify data load errors
         */
        this.subscribeTo(userExternalListingsList.onDataError, (err) => {
            showError({
                title: 'There was an error loading your external listings',
                error: err
            });
        });

        /**
         * Load suggestions.
         */
        this.subscribeTo(suggestedPlatformsList.onData, this.suggestedPlatforms);

        /// Notify data load errors
        this.subscribeTo(suggestedPlatformsList.onDataError, (err) => {
            showError({
                title: 'There was an error loading the platforms',
                error: err
            });
        });

        // When we have an ID, we need to load it first
        if (this.earningsEntry.earningsEntryID()) {

            this.isLoading(true);

            this.dataManager.onceLoaded()
            .then((data) => {
                this.isLoading(false);
                if (this.editorMode() === EditorMode.copy) {
                    // On copy mode, we need to reset the ID and dataManager
                    // so it forces to create a new entry (otherwise it will
                    // actually update the original one)
                    data.earningsEntryID = 0;
                    this.dataManager = userEarningsItem(0);
                }
                this.earningsEntry.model.updateWith(data);
                this.dataTimestamp(this.earningsEntry.model.dataTimestamp());
                // Load info about the client of this entry
                // It's optional, so careful since can be null
                if (data.clientUserID) {
                    clientsDataItem(data.clientUserID)
                    .onceLoaded()
                    .then((client) => {
                        // IMPORTANT: Be careful (again), because if something was slow
                        // loading, user could have picked (edited/added) a new client
                        // in the editor, and we do not want to replace that info,
                        // check if there is no data, or that data has the same ID
                        // that the one we have loaded
                        if (!this.selectedClient() ||
                            this.selectedClient().clientUserID === client.clientUserID) {
                            this.selectedClient(client);
                        }
                    })
                    .catch((error) => {
                        showError({
                            title: 'There was an error loading the client information at the entry',
                            error
                        });
                    });
                }
            })
            .catch((error) => {
                this.isLoading(false);
                showError({
                    title: 'There was an error loading the earnings entry',
                    error
                });
            });
        }
    }

    /**
     * Makes given client current one selected.
     * @param {rest/Client} client
     * @method
     */
    selectClient(client) {
        // Updates edited entry with the client ID selected
        this.earningsEntry.clientUserID(client.clientUserID);
        // Save a reference, to display name and other info
        this.selectedClient(client);
        this.goToSummary();
    }

    /**
     * Makes given listing the one selected
     * @param {rest/UserExternalListing} listing
     * @method
     */
    selectListing(listing) {
        this.earningsEntry.userExternalListingID(listing.userExternalListingID);
        this.earningsEntry.listingTitle(listing.title);
        // for integrity, makes platform matches listing (not needed to save the data)
        this.earningsEntry.platformID(listing.platformID);
        this.saveStep();
    }

    /**
     * Gets the job title from the given user listing and use it as the
     * selected job title of the earnings entry
     * @param {rest/UserJobTitle} listing
     * @method
     */
    selectListingJobTitle(listing) {
        this.earningsEntry.jobTitleID(listing.jobTitleID);
        this.earningsEntry.jobTitleName(listing.title);
        this.goToSummary();
    }

    /**
     * Gets the job title from a job title selected from the autocomplete
     * and use it as the selected job title of the earnings entry
     * @param {string} textValue User input text searching a job title
     * @param {models/JobTitle} data Selected job title model
     */
    selectJobTitle(textValue, data) {
        if (!data || !data.jobTitleID) return;

        const id = data.jobTitleID();
        this.earningsEntry.jobTitleID(id);
        this.earningsEntry.jobTitleName(data.singularName());
        this.goToSummary();
        return {
            value: ActionForValue.clear
        };
    }

    /**
     * Makes given platform as the selected listing, which means a listing
     * will be created for that platform
     * @param {rest/Platform} platform
     */
    selectPlatform(platform) {
        this.earningsEntry.platformID(platform.platformID);
        // resets listingID or will not take effect
        this.earningsEntry.userExternalListingID(null);
        // Something to display to the user
        this.earningsEntry.listingTitle(`My ${platform.name} listing`);
        this.saveStep();
    }

    /**
     * Save data in the server
     * @returns {Promise<object>}
     */
    save() {
        if (this.isSaving()) return Promise.reject();

        this.isSaving(true);

        const data = this.earningsEntry.model.toPlainObject(true);

        return this.dataManager
        .save(data)
        .then((freshData) => {
            this.isSaving(false);
            if (this.onSaved) {
                // Notify
                this.onSaved(freshData);
            }
            else {
                // Use updated/created data
                this.earningsEntry.model.updateWith(freshData);
                this.dataTimestamp(this.earningsEntry.model.dataTimestamp());
            }
        })
        .catch((error) => {
            this.isSaving(false);
            showError({
                title: 'There was an error saving the earnings entry',
                error
            });
        });
    }

    /**
     * Delete the entry being edited after confirmation
     * @returns {Promise}
     */
    confirmDelete() {
        if (this.isDeleting()) return Promise.reject();

        this.isDeleting(true);
        const id = this.earningsEntry.earningsEntryID();

        return showConfirm({
            title: 'Are you sure?',
            message: 'Delete ernings entry #' + id,
            yes: 'Delete',
            no: 'Keep'
        })
        .then(() =>  this.dataManager.delete())
        .then(() => {
            this.isDeleting(false);
            if (this.onDeleted) {
                // Notify
                this.onDeleted();
            }
            else {
                // Reset to new item
                this.earningsEntry.model.reset();
                this.editorMode(EditorMode.add);
                this.dataManager = userEarningsItem(0);
            }
        })
        .catch((error) => {
            this.isDeleting(false);
            if (error) {
                showError({
                    title: 'There was an error deleting the earnings entry',
                    error
                });
            }
        });
    }
}

ko.components.register(TAG_NAME, EarningsEditor);

