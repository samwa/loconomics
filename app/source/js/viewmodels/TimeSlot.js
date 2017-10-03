/**
    TimeSlot view model (aka: CalendarSlot) for use
    as part of the template/component time-slot-tile or activities
    providing data for the template.
**/
'use strict';

var getObservable = require('../utils/getObservable');
var numeral = require('numeral');
var Appointment = require('../models/Appointment');
var ko = require('knockout');
var moment = require('moment');

function TimeSlotViewModel(params) {
    /*jshint maxcomplexity:9*/

    this.startTime = getObservable(params.startTime || null);
    this.endTime = getObservable(params.endTime || null);
    this.subject = getObservable(params.subject || null);
    this.description = getObservable(params.description || null);
    this.link = getObservable(params.link || null);
    this.actionIcon = getObservable(params.actionIcon || null);
    this.actionText = getObservable(params.actionText || null);
    this.classNames = getObservable(params.classNames || null);
    this.apt = getObservable(params.apt);

    this.ariaLabel = ko.pureComputed(function(){
        var apt = this.apt();
        if (!apt) return '';

        var start = moment(this.startTime()).format('h:mma');
        var end = moment(this.endTime()).format('h:mma');
        var clickAction = '';
        if (Appointment.specialIds.free === apt.id()) {
            clickAction = ' Click to add a new booking or calendar block';
        }
        else if (apt.id() > 0) {
            // Is an event: a booking or any other type like calendar block
            // or imported event
            if (apt.sourceBooking()) {
                clickAction = ' Click to see booking details';
            }
            else {
                clickAction = ' Click to see event details';
            }
        }
        return this.subject() + ' from ' + start + ' until ' + end + clickAction;
    }, this);
}

module.exports = TimeSlotViewModel;

/**
    Static constructor to convert an Appointment model into 
    a TimeSlot instance following UI criteria for preset values/setup.
**/
TimeSlotViewModel.fromAppointment = function fromAppointment(apt) {
    /*jshint maxcomplexity:10 */
    
    // Commented the option to detect and not link unavail slots:
    //var unavail = Appointment.specialIds.unavailable === apt.id();
    //var link = null;
    //if (!unavail)
    var link = '#!appointment/' + apt.startTime().toISOString() + '/' + apt.id();
    
    if (apt.id() === Appointment.specialIds.preparationTime) {
        // Special link case: it goes to scheduling preferences to allow quick edit
        // the preparation time slots
        link = '#!schedulingPreferences?mustReturn=1';
    }

    var classNames = null;
    if (Appointment.specialIds.free === apt.id()) {
        classNames = 'Tile--tag-gray-lighter ';
    }
    else if (apt.id() > 0 && apt.sourceBooking()) {
        if (apt.sourceBooking().isRequest())
            classNames = 'Tile--tag-warning ';
        else
            classNames = 'Tile--tag-primary ';

        classNames += 'ItemAddonTile--largerContent ';
    }
    else {
        // any block event, preparation time slots
        classNames = 'Tile--tag-danger ';
    }

    return new TimeSlotViewModel({
        startTime: apt.startTime,
        endTime: apt.endTime,
        subject: apt.summary,
        description: apt.description,
        link: link,
        apt: apt,
        actionIcon: (apt.sourceBooking() ? null : apt.sourceEvent() ? 'fa ion ion-ios-arrow-right' : !apt.id() ? 'fa ion ion-plus' : null),
        actionText: (
            apt.sourceBooking() && 
            apt.sourceBooking().pricingSummary() ? 
            numeral(apt.sourceBooking().pricingSummary().totalPrice() || 0).format('$0.00') :
            null
        ),
        classNames: classNames
    });
};
