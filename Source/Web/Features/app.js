import { PLATFORM } from 'aurelia-pal';
import style from '../styles/style.scss';

export class index {
    constructor() {

    }

    configureRouter(config, router) {
        config.options.pushState = true;
        config.map([
            { route: ['', 'welcome'], name: 'welcome', moduleId: PLATFORM.moduleName('welcome') },
            { route: ':id/Accounts/Login', name: 'Login', moduleId: PLATFORM.moduleName('Accounts/Login') },
            { route: ':id/Accounts/Consent', name: 'Consent', moduleId: PLATFORM.moduleName('Accounts/Consent') },
            { route: ':id/Registration/SelfRegistration', name: 'SelfRegistration', moduleId: PLATFORM.moduleName('Registration/SelfRegistration') }
        ]);
        this.router = router;
    }
}