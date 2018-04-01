import { PLATFORM } from 'aurelia-pal';
import style from '../styles/style.scss';
import { OpenIdConnect, OpenIdConnectRoles } from "aurelia-open-id-connect";
import { inject } from 'aurelia-dependency-injection';

@inject(OpenIdConnect)
export class app {
    constructor(openIdConnect) {
        this._openIdConnect = openIdConnect;
    }

    configureRouter(config, router) {
        config.options.pushState = true;
        config.map([
            { route: ['', 'welcome'], name: 'welcome', moduleId: PLATFORM.moduleName('welcome') },
            { route: ':tenant/Accounts/Login', name: 'Login', moduleId: PLATFORM.moduleName('Accounts/Login') },
            { route: ':tenant/Accounts/Consent', name: 'Consent', moduleId: PLATFORM.moduleName('Accounts/Consent') },
            { route: ':tenant/:application/Registration/RequestAccess', name: 'RequestAccess', moduleId: PLATFORM.moduleName('Registration/RequestAccess') },
            { route: 'Registration/RequestAccessOidcCallback', name: 'RequestAccessOidcCallback', moduleId: PLATFORM.moduleName('Registration/RequestAccessOidcCallback') }
        ]);

        this._openIdConnect.configure(config);

        this.router = router;
    }
}