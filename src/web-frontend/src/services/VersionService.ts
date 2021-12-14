import axios, { AxiosInstance, AxiosRequestConfig } from 'axios';
import VersionInfo from '@/models/VersionInfo';

export default class VersionService {

    readonly versionEndPpoint = "/v3/d491853d-cf15-4eef-82b4-199a38274c27";

    async getVersion() {
        const url = `${process.env.VUE_APP_BASEURL}${this.versionEndPpoint}`;
        console.debug(`Loading version number from ${url}`);
        return await axios.get<VersionInfo>(url);
    };
}

export const versionService = new VersionService();