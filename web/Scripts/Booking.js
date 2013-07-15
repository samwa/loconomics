﻿/* Author: Loconomics */
// OUR namespace (abbreviated Loconomics)
var LC = window['LC'] || {};

function bookingChangeLocation() {
    var sel = $(this);
    // :hidden selectors is a hack because jQuery doesn't
    // hide elements that are inside a hidden parent.
    if (sel.val() == "0") {
        sel.siblings('.enter-new-location').show('fast');
    } else {
        sel.siblings('.enter-new-location, .enter-new-location:hidden').hide('fast');
    }
}
/* DEPRECATED: use LC.timeSpan defined at script.js, ever available */
function lcTime (hour, minute, second) {
    this.hour = Math.floor(parseFloat(hour || 0));
    this.minute = Math.floor(parseFloat(minute || 0));
    this.second = Math.floor(parseFloat(second || 0));

    this.toString = function () {
        var h = this.hour.toString();
        if (h.length == 1)
            h = '0' + h;
        var m = this.minute.toString();
        if (m.length == 1)
            m = '0' + m;
        var s = this.second.toString();
        if (s.length == 1)
            s = '0' + s;
        return h + lcTime.delimiter + m + lcTime.delimiter + s;
    };
    /* Print only hours and minutes in the common time of the day format
        (AM/PM in US, 24h on ES)
       TODO: Needs migration to LC.timeSpan
    */
    this.toDayTimeString = function () {
        switch ($('html').attr('lang')) {
            case 'es':
                var h = this.hour.toString();
                if (h.length == 1)
                    h = '0' + h;
                var m = this.minute.toString();
                if (m.length == 1)
                    m = '0' + m;
                return h + lcTime.delimiter + m;
            case 'en':
            default:
                var h = this.hour.toString();
                var sufix = ' AM';
                if (h >= 12 && h < 24)
                    sufix = ' PM';
                if (h > 12)
                    h = h % 12;
                if (h == 0)
                    h = 12;
                var m = this.minute.toString();
                if (m.length == 1)
                    m = '0' + m;
                return h + lcTime.delimiter + m + sufix;
        }
    };
    /* TODO: Needs migration to LC.timeSpan
    */
    this.addHours = function (hours) {
        this.addMinutes(hours * 60);
        return this;
    };
    /* TODO: Needs migration to LC.timeSpan
    */
    this.addMinutes = function (minutes) {
        var m = this.hour * 60 + this.minute;
        var ntime = minutes + m;
        this.hour = Math.floor(ntime / 60);
        this.minute = ntime % 60;
        return this;
    };
};
// For spanish and english works good ':' as delimiter
lcTime.delimiter = ':';
lcTime.parse = function (strtime) {
    strtime = (strtime || '').split(this.delimiter);
    if (strtime.length > 1) {
        return new lcTime(strtime[0], strtime[1], (strtime.length > 2 ? strtime[2] : 0));
    }
    return null;
};

LC.showDateHours = function (date) {
    // Load date hours:
    var $day = $('#dayHoursSelector');
    var strdate = LC.dateToInterchangleString(date);
    var minutes = $day.data('duration-minutes');
    var userid = $day.data('user-id');
    $day.reload({
        url: LcUrl.LangPath + "Booking/$ScheduleCalendarElements/DayHoursSelector/" +
            encodeURIComponent(strdate) + '/' + minutes + '/' + userid + '/',
        complete: LC.markSelectedDates,
        autofocus: false
    });
    $day.data('date', date);
};
LC.showWeek = function (date) {
    var $week = $('#weekDaySelector');

    if (/(previous|next)/.test(date.toString())) {
        var x = date;
        date = new Date($week.data('date'));
        if (/previous/.test(x))
            date.setDate(date.getDate() - 7);
        else if (/next/.test(x))
            date.setDate(date.getDate() + 7);
    }

    var strdate = LC.dateToInterchangleString(date);
    $week.reload({
        url: LcUrl.LangPath + "Booking/$ScheduleCalendarElements/WeekDaySelector/" +
            encodeURIComponent(strdate) + '/',
        complete: function () { LC.selectWeekDay(date); LC.showDateHours(date); },
        autofocus: false 
    });
    $week.data('date', date);
};
LC.selectWeekDay = function (date) {
    var $week = $('#weekDaySelector');
    // Mark selected day in calendar
    $week.find('.day-selection-action').removeClass('current')
    .filter(function () {
        var elDate = new Date($(this).data('date'));
        return (elDate.toDateString() == date.toDateString());
    }).addClass('current');
};
LC.setupScheduleCalendar = function () {
    var $scheduleStep = $('#booking-schedule')
    .on('click', '#weekDaySelector .week-slider', function () {

        LC.showWeek(this.getAttribute('href').substring(1));

        return false;
    })
    .on('click', '#weekDaySelector .day-selection-action', function () {
        var date = new Date($(this).data('date'));

        LC.showDateHours(date);
        LC.selectWeekDay(date);

        return false;
    })
    .on('change', '#dayHoursSelector select.choice-selector', function () {
        var $s = $(this), v = $s.val();
        if (v) {
            // Get row date and time
            var $row = $s.closest('tr');
            var $table = $row.closest('table');
            var start = $row.find('.start').text();
            var end = $row.find('.end').text();
            var date = new Date($table.data('date'));
            var dateshowed = $table.find('caption').text();

            // Show date and time
            var choice = $(this).closest('form').find('.selected-schedule').find('.' + v + '-choice');
            choice.addClass('has-values');
            choice.find('span.date-showed').text(dateshowed);
            choice.find('span.start-time').text(start);
            choice.find('span.end-time').text(end);
            choice.find('input.date').val(LC.dateToInterchangleString(date));
            choice.find('input.start-time').val(start);

            // Others Select with this same option selected must be reset:
            $s.closest('tr').siblings().find('option[value=' + v + ']:selected').closest('select').val('');
        }
    })
    .on('click', '.unselect-action', function () {
        // Remove values from view-selection panel and form
        $(this).siblings('span').text('')
        .closest('.has-values').removeClass('has-values')
        .find('input').val('');
        // Remove selection from day-hours panel
        var v = $(this).closest('.choice').data('choice');
        $('#dayHoursSelector').find('.selector option[value=' + v + ']').prop('selected', false);
        return false;
    });

    // Select current day in week selector
    LC.selectWeekDay(new Date($('#dayHoursSelector').data('date')));
};
LC.getSelectedDates = function (filterDate) {
    var selected = { first: 0, second: 0, third: 0 };
    for (var v in selected) {
        var choice = $('.selected-schedule').find('.' + v + '-choice');
        var d = choice.find('input.date').val();
        var t = choice.find('input.start-time').val();
        if (filterDate == d)
            selected[v] = { Date: d, Time: t };
    }
    return selected;
};
LC.markSelectedDates = function () {
    var $day = $('#dayHoursSelector');
    var selected = LC.getSelectedDates(LC.dateToInterchangleString(new Date($day.data('date'))));
    $day.find('tr > .start').each(function () {
        for (var v in selected)
            if (selected[v] && $(this).text() == selected[v].Time)
                $(this).siblings('.selector').find('option[value=' + v + ']').prop('selected', true);
    });
};
LC.setupServiceMap = function () {
    LC.mapReady(function () {
        $('.serviceRadiusMap').each(function () {
            var m = $(this);
            var radius = m.data('service-radius');
            var latlng = new google.maps.LatLng(parseFloat(m.data('latitude')), parseFloat(m.data('longitude')));
            //latlng = new google.maps.LatLng(37.75334439226298, -122.4254606035156);
            var mapOptions = {
                zoom: 10,
                center: latlng,
                mapTypeId: google.maps.MapTypeId.ROADMAP
            }
            var map = new google.maps.Map(m.get(0), mapOptions);
            var radiusUnit = LC.distanceUnits[LC.getCurrentCulture().country];
            var circle = new google.maps.Circle({
                center: latlng,
                map: map,
                clickable: false,
                radius: (radiusUnit == 'miles' ? convertMilesKm(radius, radiusUnit) : radius) * 1000, // in meters
                fillColor: '#00989A',
                fillOpacity: .3,
                strokeWeight: 0
            });
        });
    });
};

LC.initScheduleStep = function () {
    var tab = $('#schedule');

    // Execute first time when showing the step
    $.proxy(bookingChangeLocation, $('.select-location'))();
    $('.select-location').change(bookingChangeLocation);

    applyDatePicker(tab);

    // Read current hidden date to be set in show date (previous apply datepicker options)
    var date = $('#hideDate').val();

    $("#showDate")
    .datepicker('setDate', date)
    .datepicker('option', 'dateFormat', 'DD, M d, yy')
    .datepicker('option', 'altField', $('#hideDate'))
    .datepicker('option', 'altFormat', $.datepicker._defaults.dateFormat)
    .datepicker('option', 'numberOfMonths', 2)
    .datepicker('option', 'onSelect', function () {
        // Hour is added with GMT (+0000) to avoid problems on getting date because by
        // default javascript adds the local time zone
        var date = new Date($('#hideDate').val() + ' 00:00:00 GMT');
        LC.showDateHours(date);
        LC.showWeek(date);
    });

    LC.setupScheduleCalendar();

    LC.setupServiceMap();
};
// Sliders on Housekeeper price:
LC.initCustomerPackageSliders = function () {
    /* Houseekeeper pricing */
    function updateAverage($c, value) {
        // Update input value and trigger change event to notify standard listeners
        $c.find('input').val(value).change();
    }
    function calculateHousekeeperPackage($sliderContent) {
        // Look for the housekeeper pricing container for this package
        // that contains all fields for calculation
        var calcContext = $sliderContent.closest('.housekeeper-pricing');
        // Get the package container, where contents to be update are
        var pak = calcContext.closest('tr');
        // Calculation formula for housekeeper pricing
        var formula = function (numbedrooms, numbathrooms) {
            var formulaA = parseFloat(calcContext.data('formula-a')),
                formulaB = parseFloat(calcContext.data('formula-b')),
                formulaC = parseFloat(calcContext.data('formula-c')),
                rate = parseFloat(calcContext.data('provider-rate'));
            // It returns a time in float minutes
            return (formulaA * numbedrooms + formulaB * numbathrooms + formulaC) * rate;
        };
        // Getting var-values from form and calculating
        var numbedrooms = calcContext.find('.housekeeper-pricing-bedrooms input').val(),
            numbathrooms = calcContext.find('.housekeeper-pricing-bathrooms input').val();
        // ...Gets duration rounded up to quarter-hours.
        var duration = LC.roundTimeToQuarterHour(
        // ..create a time object..
            LC.timeSpan.fromMinutes(
        // Computes formula with customer values...
                formula(numbedrooms, numbathrooms)
            )
        , LC.roundingTypeEnum.Up);
        // Updating user-viewed time, show it in the smart way
        pak.find('.package-duration').text(duration.toSmartString());
        // Recalculating price with new time, using the package hourly-rate
        var hourlyRate = parseFloat(calcContext.data('hourly-rate'));
        var hourlyFee = parseFloat(calcContext.data('hourly-fee'));
        // Calculate the price for the total time,
        // with the hourleRate (already with fees), hourlyFeeAmount
        // and the already rounded duration:
        var totalTimePrice = LC.calculateHourlyPrice(duration, hourlyRate, hourlyFee);
        // Set new item-price and trigger a change event to allow the items-fees calculation
        // system do their job and showing the total price
        LC.setMoneyNumber(totalTimePrice.totalPrice, pak.find('.calculate-item-price'));
        LC.setMoneyNumber(totalTimePrice.feePrice, pak.find('.calculate-item-fee'));
        pak.find('.calculate-item-price').trigger('change');
    }
    $(".customer-slider").each(function () {
        var $c = $(this);
        // Creating sliders
        var average = $c.data('slider-value'),
            step = $c.data('slider-step') || 1;
        if (!average) return;
        var slider = $('<div class="slider"/>').appendTo($c);
        var setup = {
            range: "min",
            value: average,
            min: average - 3 * step,
            max: average + 3 * step,
            step: step,
            change: function (event, ui) {
                updateAverage($c, ui.value);
            }
        };
        slider.slider(setup);
        LC.createLabelsForUISlider(slider);
        // Setup the input field, hidden and with initial value synchronized with slider
        var field = $c.find('input');
        field.hide();
        var currentValue = field.val() || average;
        updateAverage($c, currentValue);
        slider.slider('value', currentValue);

        // Perform calculation of time-prices for the package on
        // init and on variables changes
        calculateHousekeeperPackage($c);
        $c.on('change', 'input', function () { calculateHousekeeperPackage($c) });

        // Switching sliders visualization on active package
        $('.pricing-wizard .packages-list input[name="provider-package"]').change(function () {
            var $t = $(this),
                row = $t.closest('tr');
            if ($t.is(':checked'))
                row.find('.package-extra-data').slideDown('fast');
            else
                row.find('.package-extra-data').slideUp('fast');
        }).change();
    });
};

$(document).ready(function () {
    LC.initCustomerPackageSliders();

    // Setup Schedule step:
    $('#schedule').on('endLoadWizardStep', function () {
        // Getting the tab content for payment that is not loaded at the start
        var tab = $('#schedule');

        // Loading, with retard
        var loadingtimer = setTimeout(function () {
            tab.block(loadingBlock);
        }, gLoadingRetard);

        $.ajax({
            // Request $Payment partial page, with all the original url parameters
            url: LcUrl.LangPath + "Booking/$Schedule/" + location.search,
            type: 'GET',
            success: function (data, text, jx) {
                // load tab content
                tab.html(data);

                LC.initScheduleStep();
            },
            error: ajaxErrorPopupHandler,
            complete: function () {
                // Disable loading
                clearTimeout(loadingtimer);
                // Unblock
                tab.unblock();
            }
        });
    }).on('reloadedHtmlWizardStep', function () {
        LC.initScheduleStep();
    });

    // Load payment content on step change:
    $('#payment').bind('endLoadWizardStep', function () {
        // Getting the tab content for payment that is not loaded at the start
        var paymentTab = $('#payment');

        // Loading, with retard
        var loadingtimer = setTimeout(function () {
            paymentTab.block(loadingBlock);
        }, gLoadingRetard);

        $.ajax({
            // Request $Payment partial page, with all the original url parameters
            url: LcUrl.LangPath + "Booking/$Payment/" + location.search,
            type: 'GET',
            success: function (data, text, jx) {
                // load tab content
                paymentTab.html(data);
            },
            error: ajaxErrorPopupHandler,
            complete: function () {
                // Disable loading
                clearTimeout(loadingtimer);
                // Unblock
                paymentTab.unblock();
            }
        });
    });
});
