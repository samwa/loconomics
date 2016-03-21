/**
    PricingSummaryVM: similar to PricingSummary Model but with autocalculation
    for some of the fields, for client-side preview.
**/
'use strict';

var Model = require('../models/Model');
var ko = require('knockout');

var numeral = require('numeral');
var PricingSummaryDetail = require('../models/PricingSummaryDetail');
var PricingSummary = require('../models/PricingSummary');

function PricingSummaryVM(values) {

    Model(this);

    this.model.defProperties({
        // Fields for use when loading data
        pricingSummaryID: null,
        pricingSummaryRevision: null,
        createdDate: null,
        updatedDate: null,
        // User Data
        details: {
            isArray: true,
            Model: PricingSummaryDetail
        },
        gratuityPercentage: 0,
        gratuityAmount: 0,
        // Data from server
        firstTimeServiceFeeFixed: 0,
        firstTimeServiceFeePercentage: 0,
        firstTimeServiceFeeMaximum: 0,
        firstTimeServiceFeeMinimum: 0,
        paymentProcessingFeePercentage: 0,
        paymentProcessingFeeFixed: 0,
        
        // Server calculated only
        serviceFeeAmount: 0,
        cancellationFeeCharged: null,
        cancellationDate: null
    }, values);

    /// Locally calculated fields (they have a persistent equivalent on server)
    
    this.subtotalPrice = ko.pureComputed(function() {
        return this.details().reduce(function(total, item) {
            total += item.price();
            return total;
        }, 0);
    }, this);
    
    this.clientServiceFeePrice = ko.pureComputed(function() {
        var t = +this.subtotalPrice(),
            f = +this.firstTimeServiceFeeFixed(),
            p = +this.firstTimeServiceFeePercentage(),
            min = +this.firstTimeServiceFeeMinimum(),
            max = +this.firstTimeServiceFeeMaximum();
        var a = Math.round((f + ((p / 100) * t)) * 100) / 100;
        return Math.min(Math.max(a, min), max);
    }, this);
    
    this.gratuity = ko.pureComputed(function() {
        var percentage = this.gratuityPercentage() |0,
            amount = this.gratuityAmount() |0;
        return (
            percentage > 0 ?
                (this.subtotalPrice() * (percentage / 100)) :
                amount < 0 ? 0 : amount
        );
    }, this);

    this.totalPrice = ko.pureComputed(function() {
        return this.subtotalPrice() + this.clientServiceFeePrice() + this.gratuity();
    }, this);

    this.serviceDurationMinutes = ko.pureComputed(function() {
        return this.details().reduce(function(total, item) {
            total += item.serviceDurationMinutes();
            return total;
        }, 0);
    }, this);
    
    this.firstSessionDurationMinutes = ko.pureComputed(function() {
        return this.details().reduce(function(total, item) {
            total += item.firstSessionDurationMinutes();
            return total;
        }, 0);
    }, this);
    
    /// Useful computed observables
    
    this.feesMessage = ko.pureComputed(function() {
        var f = numeral(this.clientServiceFeePrice()).format('$#,##0.00');
        return '*The first-time __fees__ booking fee applies only to the very first time you connect with a professional and helps cover costs of running the marketplace. There are no fees for subsequent bookings with this professional.'.replace(/__fees__/g, f);
    }, this);

    this.items = ko.pureComputed(function() {

        var items = this.details().slice();
        var gratuity = this.gratuity();

        if (gratuity > 0) {
            var gratuityLabel = this.gratuityPercentage() ?
                'Gratuity (__gratuity__%)'.replace(/__gratuity__/g, (this.gratuityPercentage() |0)) :
                'Gratuity';

            items.push(new PricingSummaryDetail({
                serviceName: gratuityLabel,
                price: gratuity
            }));
        }
        
        var fees = this.clientServiceFeePrice();
        if (fees > 0) {
            var feesLabel = 'First-time booking fee*';
            items.push(new PricingSummaryDetail({
                serviceName: feesLabel,
                price: fees
            }));
        }

        return items;
    }, this);
    
    var duration2Language = require('../utils/duration2Language');
    
    this.serviceDurationDisplay = ko.pureComputed(function() {
        return duration2Language({ minutes: this.serviceDurationMinutes() });
    }, this);
    
    this.firstSessionDurationDisplay = ko.pureComputed(function() {
        return duration2Language({ minutes: this.firstSessionDurationMinutes() });
    }, this);
    
    /// Export data
    
    this.toPricingSummary = function() {
        var plain = this.model.toPlainObject(true);
        plain.subtotalPrice = this.subtotalPrice();
        plain.clientServiceFeePrice = this.clientServiceFeePrice();
        plain.totalPrice = this.totalPrice();
        plain.serviceDurationMinutes = this.serviceDurationMinutes();
        plain.firstSessionDurationMinutes = this.firstSessionDurationMinutes();
        return new PricingSummary(plain);
    };
}

module.exports = PricingSummaryVM;
