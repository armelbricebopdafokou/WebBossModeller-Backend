const ldap = require('ldapjs');

// LDAP Configuration
const LDAP_OPTS = {
    server: {
        url: 'ldaps://ods0.hs-bochum.de:636',
        searchBase: 'o=hs-bochum.de,o=isp',
        searchFilter: '(uid={{username}})', // Placeholder for dynamic username
    }
};

// Function to authenticate and search
function authenticateAndSearch(username: string, password: string) {
    const client = ldap.createClient({
        url: LDAP_OPTS.server.url,
    });

    // Bind to the LDAP server using the provided username and password
    client.bind(`uid=${username},${LDAP_OPTS.server.searchBase}`, password, (err:  any) => {
        if (err) {
            console.error('Failed to bind:', err.message);
            client.unbind();
            return;
        }
        console.log('Authentication successful!');

        // Perform the search
        const searchFilter = LDAP_OPTS.server.searchFilter.replace('{{username}}', username);
        const searchOptions = {
            filter: searchFilter,
            scope: 'sub',
            attributes: ['cn', 'mail'], // Specify desired attributes
        };

        client.search(LDAP_OPTS.server.searchBase, searchOptions, (err:any, res:any) => {
            if (err) {
                console.error('Search failed:', err.message);
                client.unbind();
                return;
            }

            res.on('searchEntry', (entry:any) => {
                console.log('Search Result:', entry.object);
            });

            res.on('searchReference', (ref:any) => {
                console.log('Search Referral:', ref.uris.join());
            });

            res.on('error', (err:any) => {
                console.error('Search error:', err.message);
            });

            res.on('end', (result:any) => {
                console.log('Search completed with status:', result.status);
                client.unbind(); // Close the connection
            });
        });
    });
}

// Example usage
const username = 'brbo9264'; // Replace with actual username
const password = '12345S@leil'; // Replace with actual password
authenticateAndSearch(username, password);
