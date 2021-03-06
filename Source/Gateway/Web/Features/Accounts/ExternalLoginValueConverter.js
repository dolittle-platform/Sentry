/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Dolittle. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

/**
 * A value converter that takes an authority and generates a correct URL for external login providers
 */
export class ExternalLoginValueConverter {
  /**
   * Convert from an authority to a absolute URL path for logging in with external provider
   * @param {*} authority
   */
  toView(authority) {
    let url = `${window.location.origin}/api/Accounts/ExternalLogin?tenant=${authority.tenant}&application=${
      authority.application
    }&authorityId=${authority.id}&authority=${authority.type}&returnUrl=${authority.returnUrl}`;
    return url;
  }
}
