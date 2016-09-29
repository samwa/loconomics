/** Help: help and FAQs articles from remote Zendesk REST API.
    Different from other 'appmodels' since does not use the common rest client
    and our server endpoint.
**/
'use strict';
var ko = require('knockout');

var articlesUrl = 'https://loconomics.zendesk.com/api/v2/help_center/articles.json?label_names=';
var categoriesUrl = 'https://loconomics.zendesk.com/api/v2/help_center/categories.json';
var sectionsUrl = 'https://loconomics.zendesk.com/api/v2/help_center/sections.json';

function getArticlesUrl(labels) {
    return articlesUrl + encodeURIComponent(labels);
}

var $ = require('jquery');

function getRemoteArticles(labels) {
    return Promise.resolve($.get(getArticlesUrl(labels))).then(function(data) {
        return data.articles || [];
    });
}
function getRemoteCategories() {
    return Promise.resolve($.get(categoriesUrl)).then(function(data) {
        return data.categories || [];
    });
}
function getRemoteSections() {
    return Promise.resolve($.get(sectionsUrl)).then(function(data) {
        return data.sections || [];
    });
}

var CacheControl = require('../utils/CacheControl');

exports.create = function create(appModel) {
    /// The in-memory cache
    var ttl = { days: 1 };
    var cache = {
        categories: new CacheControl({ ttl: ttl }),
        sections: new CacheControl({ ttl: ttl }),
        articles: { /*CacheControl by labels*/ },
        clear: function() {
            this.categories.reset();
            this.categories.data = null;
            this.sections.reset();
            this.sections.data = null;
            this.articles = {};
        },
        getArticlesCache: function(labels) {
            var cacheItem = this.articles[labels];
            if (!cacheItem) {
                cacheItem = this.articles[labels] = new CacheControl({ ttl: ttl });
            }
            return cacheItem;
        },
        setArticles: function(labels, data) {
            var cacheItem = this.getArticlesCache(labels);
            // If didn't exist, it was already created by the previous call
            // just update data and time
            cacheItem.data = data;
            cacheItem.latest = new Date();
            return cacheItem;
        }
    };
    
    /// The exposed API
    var api = {};
    api.clearCache = function() {
        cache.clear();
    };

    appModel.on('clearLocalData', function() {
        api.clearCache();
    });
    
    var HelpCategory = require('../models/HelpCategory');
    api.isLoadingCategories = ko.observable(false);
    api.getCategories = function() {
        if (cache.categories.mustRevalidate()) {
            api.isLoadingCategories(true);
            return getRemoteCategories().then(function(data) {
                cache.categories.data = data.map(function(a) {
                    return new HelpCategory(a);
                });
                cache.categories.latest = new Date();
                api.isLoadingCategories(false);
                return cache.categories.data;
            }, function(err) {
                api.isLoadingCategories(false);
                throw err;
            });
        }
        else {
            return Promise.resolve(cache.categories.data);
        }
    };
    
    var HelpSection = require('../models/HelpSection');
    api.isLoadingSections = ko.observable(false);
    api.getSections = function() {
        if (cache.sections.mustRevalidate()) {
            api.isLoadingSections(true);
            return getRemoteSections().then(function(data) {
                cache.sections.data = data.map(function(a) {
                    return new HelpSection(a);
                });
                cache.sections.latest = new Date();
                api.isLoadingSections(false);
                return cache.sections.data;
            }, function(err) {
                api.isLoadingSections(false);
                throw err;
            });
        }
        else {
            return Promise.resolve(cache.sections.data);
        }
    };
    
    var HelpArticle = require('../models/HelpArticle');
    api.isLoadingArticles = ko.observable(false);
    api.getArticles = function(labels) {
        labels = labels || '';
        var cached = cache.getArticlesCache(labels);
        
        if (cached.mustRevalidate()) {
            api.isLoadingArticles(true);
            return getRemoteArticles(labels).then(function(data) {
                cache.setArticles(labels, data.map(function(a) {
                    return new HelpArticle(a);
                }));
                api.isLoadingArticles(false);
                return cached.data;
            }, function(err) {
                api.isLoadingArticles(false);
                throw err;
            });
        }
        else {
            return Promise.resolve(cached.data);
        }
    };

    return api;
};
