var ldap = require('ldapjs');
// LDAP Configuration
var LDAP_OPTS = {
    server: {
        url: 'ldaps://ods0.hs-bochum.de:636',
        searchBase: 'o=hs-bochum.de,o=isp',
        searchFilter: '(uid={{username}})', // Placeholder for dynamic username
    }
};
// Function to authenticate and search
function authenticateAndSearch(username, password) {
    var client = ldap.createClient({
        url: LDAP_OPTS.server.url,
    });
    // Bind to the LDAP server using the provided username and password
    client.bind("uid=".concat(username, ",").concat(LDAP_OPTS.server.searchBase), password, function (err) {
        if (err) {
            console.error('Failed to bind:', err.message);
            client.unbind();
            return;
        }
        console.log('Authentication successful!');
        // Perform the search
        var searchFilter = LDAP_OPTS.server.searchFilter.replace('{{username}}', username);
        var searchOptions = {
            filter: searchFilter,
            scope: 'sub',
            attributes: ['cn', 'mail'], // Specify desired attributes
        };
        client.search(LDAP_OPTS.server.searchBase, searchOptions, function (err, res) {
            if (err) {
                console.error('Search failed:', err.message);
                client.unbind();
                return;
            }
            res.on('searchEntry', function (entry) {
                console.log('Search Result:', entry.object);
            });
            res.on('searchReference', function (ref) {
                console.log('Search Referral:', ref.uris.join());
            });
            res.on('error', function (err) {
                console.error('Search error:', err.message);
            });
            res.on('end', function (result) {
                console.log('Search completed with status:', result.status);
                client.unbind(); // Close the connection
            });
        });
    });
}
// Example usage
var username = 'brbo9264'; // Replace with actual username
var password = '12345S@leil'; // Replace with actual password
authenticateAndSearch(username, password);
