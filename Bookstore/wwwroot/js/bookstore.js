/*
Bookstore.JS Notes:
- Only included on the main bookmark search/query page
- Utilizes cash.js for jquery-like code
- Implements the following features:
    - "Select All" checkbox
    - Hide table rows when typing into filter bar
    - Selecting background of table row should activate associated checkbox
    - Disable select buttons when table rows are checked/unchecked
 */


// Cash.js extension: short circuiting any()
$.fn.any = function(predicate){
    for (var i=0; i<this.length; i++) {
        if (predicate(i, this[i]) === true) {
            return true;
        }
    }
    return false;
};


$(document).ready(function() {
    // Disable control buttons when unchecked
    var enableOnCheckControls = $('#bookstore-control-archive, #bookstore-control-delete, #bookstore-control-edit');
    var refreshControls = function() {
        var anyChecked = $('#bookstore-table-body input[type="checkbox"]').any((i, e) =>  e.checked);
        enableOnCheckControls.prop('disabled', !anyChecked);
    }
    $('#bookstore-table-body input[type="checkbox"]').on(refreshControls);
    refreshControls();
    
    /// Select all checkbox
    var selectAllToggle = false;
    $('#bookstore-control-select-all').on('click', function(event) {
        selectAllToggle = !selectAllToggle;
        $('#bookstore-table input[type="checkbox"]').prop('checked', selectAllToggle);
        refreshControls();
    });

    /// Filter bookmarks input
    $('#bookstore-control-filter').on('input', function(event) {
        var filterText = event.target.value.toLowerCase();
        
        if (filterText.length === 0) {
            $('#bookstore-table-body tr').removeClass('d-none');
        }
        
        var matchesFilter = function(index, htmlElement) {
            var element = $(htmlElement);
            
            // Check if the link title or url matches
            var link = element.find('.bookstore-table-link').first();
            if (link.attr('title').toLowerCase().includes(filterText) ||
                link.attr('href').toLowerCase().includes(filterText)) {
                return true;
            }
            
            var textContentMatchPredicate = function(index, element) {
                return element.textContent.toLowerCase().includes(filterText);
            }

            // Check matching folder text content
            if (element.find('.bookstore-table-folder').any(textContentMatchPredicate)) {
                return true;
            }

            // Check matching tags text content
            if (element.find('.bookstore-table-tag').any(textContentMatchPredicate)) {
                return true;
            }
            
            return false;
        }

        $('#bookstore-table-body tr')
            .addClass('d-none')
            .filter(matchesFilter)
            .removeClass('d-none');
    });
    
    /// Table Row Background Check
    $('#bookstore-table-body tr').on('click', function(event) {
        if (event.target.tagName !== 'input') {
            var element = $(event.target);
            var row = (element.tagName === 'tr') ? element : element.parent('tr');
            var checkbox = row.find('input[type="checkbox"]');
            checkbox.prop('checked', !checkbox.prop('checked'));
            refreshControls();
        }
    });
    
});