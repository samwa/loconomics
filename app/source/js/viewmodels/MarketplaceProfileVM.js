'use strict';
// IMPORTANT: it requires access to DOM with jQuery in order to the COPY LINK to work on browsers
var ko = require('knockout');
var clipboard = require('../utils/clipboard');
var marketplaceProfile = require('../data/marketplaceProfile');

module.exports = function MarketplaceProfileVM(app) {

    var profileVersion = marketplaceProfile.newVersion();
    profileVersion.isObsolete.subscribe(function(itIs) {
        if (itIs) {
            // new version from server while editing
            // FUTURE: warn about a new remote version asking
            // confirmation to load them or discard and overwrite them;
            // the same is need on save(), and on server response
            // with a 509:Conflict status (its body must contain the
            // server version).
            // Right now, just overwrite current changes with
            // remote ones:
            profileVersion.pull({ evenIfNewer: true });
        }
    });

    // Actual data for the form:
    this.profile = profileVersion.version;

    this.isLoading = marketplaceProfile.isLoading;
    this.isSaving = marketplaceProfile.isSaving;
    this.isLocked = marketplaceProfile.isLocked;

    this.discard = function discard() {
        profileVersion.pull({ evenIfNewer: true });
        this.copyCustomUrlButtonText('Copy');
    }.bind(this);

    this.save = function save() {
        return profileVersion.pushSave();
    };

    this.sync = marketplaceProfile.sync.bind(marketplaceProfile);

    /// Utilities
    // Custom URL
    this.customUrlProtocol = ko.observable('https://');
    this.customUrlDomainPrefix = ko.observable('www.loconomics.com/-');
    /**
        Autogenerated custom URL for the current 'draft' data (being edited by the user)
    **/
    this.customUrlDraft = ko.pureComputed(function() {
        return this.customUrlProtocol() + this.customUrlDomainPrefix() + this.profile.serviceProfessionalProfileUrlSlug();
    }, this);
    // Copy Custom URL
    this.copyCustomUrlButtonText = ko.observable('Copy');
    this.profile.serviceProfessionalProfileUrlSlug.subscribe(function() {
        // On any change, restore copy label
        this.copyCustomUrlButtonText('Copy');
    }.bind(this));
    this.copyCustomUrl = function() {
        var url = this.customUrlDraft();
        var errMsg = clipboard.copy(url);
        if (errMsg) {
            app.modals.showError({ error: errMsg });
        }
        else {
            this.copyCustomUrlButtonText('Copied!');
        }
    }.bind(this);
};
