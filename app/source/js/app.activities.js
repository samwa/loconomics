/**
    List of activities to preload in the App (at main entry point 'app'),
    as an object with the activity name as the key
    and the controller as value.
**/
'use strict';

module.exports = {
    'downloadApp': require('./activities/downloadApp'),
    'calendar': require('./activities/calendar'),
    'datetimePicker': require('./activities/datetimePicker'),
    'clients': require('./activities/clients'),
    'serviceProfessionalService': require('./activities/serviceProfessionalService'),
    'serviceAddresses': require('./activities/serviceAddresses'),
    'dashboard': require('./activities/dashboard'),
    'appointment': require('./activities/appointment'),
    'login': require('./activities/login'),
    'logout': require('./activities/logout'),
    'signup': require('./activities/signup'),
    'welcome': require('./activities/welcome'),
    'addressEditor': require('./activities/addressEditor'),
    'account': require('./activities/account'),
    'inbox': require('./activities/inbox'),
    'conversation': require('./activities/conversation'),
    'help': require('./activities/help'),
    'feedbackForm': require('./activities/feedbackForm'),
    'contactForm': require('./activities/contactForm'),
    'cms': require('./activities/cms'),
    'clientEditor': require('./activities/clientEditor'),
    'schedulingPreferences': require('./activities/schedulingPreferences'),
    'calendarSyncing': require('./activities/calendarSyncing'),
    'bookMeButton': require('./activities/bookMeButton'),
    'community': require('./activities/community'),
    'privacySettings': require('./activities/privacySettings'),
    'addJobTitle': require('./activities/addJobTitle'),
    'serviceProfessionalServiceEditor': require('./activities/serviceProfessionalServiceEditor'),
    'servicesOverview': require('./activities/servicesOverview'),
    'verifications': require('./activities/verifications'),
    'education': require('./activities/education'),
    'backgroundCheck': require('./activities/backgroundCheck'),
    'educationForm': require('./activities/educationForm'),
    'bookingPolicies': require('./activities/bookingPolicies'),
    'licensesCertifications': require('./activities/licensesCertifications'),
    'licensesCertificationsForm': require('./activities/licensesCertificationsForm'),
    'workPhotos': require('./activities/workPhotos'),
    'home': require('./activities/home'),
    'learnMoreProfessionals': require('./activities/learnMoreProfessionals'),
    'booking': require('./activities/booking'),
    'terms': require('./activities/terms'),
    'payments': require('./activities/payments'),
    'userFees': require('./activities/userFees'),
    'performance': require('./activities/performance'),
    'searchJobTitle': require('./activities/searchJobTitle'),
    'searchCategory': require('./activities/searchCategory'),
    'paymentAccount': require('./activities/paymentAccount'),
    'payoutPreference': require('./activities/payoutPreference'),
    'myAppointments': require('./activities/myAppointments'),
    'clientAppointment': require('./activities/clientAppointment'),
    'viewBooking': require('./activities/viewBooking'),
    'blog': require('./activities/blog'),
    'cancellationPolicies': require('./activities/cancellationPolicies'),
    'mockupHouseCleanerServiceEditor': require('./activities/mockupHouseCleanerServiceEditor'),
    'userFeePayments': require('./activities/userFeePayments'),
    'ownerAcknowledgment': require('./activities/ownerAcknowledgment'),
    'userProfile': require('./activities/userProfile'),
    'upgrade': require('./activities/upgrade'),
    'listingEditor': require('./activities/listingEditor'),
    'publicBio': require('./activities/publicBio'),
    'publicProfilePicture': require('./activities/publicProfilePicture'),
    'serviceProfessionalCustomURL': require('./activities/serviceProfessionalCustomURL'),
    'serviceProfessionalBusinessInfo': require('./activities/serviceProfessionalBusinessInfo'),
    'userBirthDay': require('./activities/userBirthDay'),
    'listing': require('./activities/listing')
};
