/*
 * Original author: ProtonMail
 * Original source: https://github.com/ProtonMail/WebClients/blob/main/packages/shared/lib/helpers/browser.ts
 * Original license: GPLv3
 * 
 * Modified by: Jack Edwards
 * Modified date: May 2024, December 2024
 */

import { UAParser } from 'ua-parser-js';

const uaParser = new UAParser();
const ua = uaParser.getResult();

export const getOS = () => {
    const { name = 'other', version = '' } = ua.os;
    return { name, version };
};

export const isIos11 = () => {
    const { name, version } = getOS();
    return name.toLowerCase() === 'ios' && parseInt(version, 10) === 11;
};

export const isAndroid = () => {
    const { name } = getOS();
    return name.toLowerCase().includes('android');
};

export const isSafari = () => ua.browser.name === 'Safari' || ua.browser.name === 'Mobile Safari';
export const isSafari11 = () => isSafari() && ua.browser.version?.startsWith('11');
export const isMinimumSafariVersion = (version: number) => isSafari() && Number(ua.browser.version) >= version;
export const isSafariMobile = () => ua.browser.name === 'Mobile Safari';
export const isIE11 = () => ua.browser.name === 'IE' && ua.browser.version?.startsWith('11');
export const isEdge = () => ua.browser.name === 'Edge';
export const isEdgeChromium = () => isEdge() && ua.engine.name === 'Blink';
export const isBrave = () => ua.browser.name === 'Brave';
export const isFirefox = () => ua.browser.name === 'Firefox';
export const isChrome = () => ua.browser.name === 'Chrome';
export const isChromiumBased = () => 'chrome' in window;
export const isJSDom = () => navigator.userAgent.includes('jsdom');
export const isMac = () => ua.os.name === 'Mac OS';
export const isWindows = () => ua.os.name === 'Windows';
export const isLinux = () => ua.ua.match(/([Ll])inux/);
export const hasTouch = typeof document === 'undefined' ? false : 'ontouchstart' in document.documentElement;
export const getBrowser = () => ua.browser;
export const getDevice = () => ua.device;

export const isMobile = () => {
    const { type } = getDevice();
    return type === 'mobile';
};

export const isDesktop = () => {
    const { type } = getDevice();
    return !type;
};

export const getIsIframe = () => window.self !== window.top;

export const isIos = () =>
    // @ts-expect-error window.MSStream cf. https://racase.com.np/javascript-how-to-detect-if-device-is-ios/
    (/iPad|iPhone|iPod/.test(navigator.userAgent) && !window.MSStream) ||
    ['iPad Simulator', 'iPhone Simulator', 'iPod Simulator', 'iPad', 'iPhone', 'iPod'].includes(navigator.platform) ||
    // iPad on iOS 13 detection
    (navigator.userAgent.includes('Mac') && 'ontouchend' in document);

export const isIpad = () => isSafari() && navigator.maxTouchPoints && navigator.maxTouchPoints > 2;
