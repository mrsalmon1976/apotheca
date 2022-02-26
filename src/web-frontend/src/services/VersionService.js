import axios from 'axios';

export default class VersionService {

    versionEndPoint = "/version";
    
    async getVersion() {
        var url = `${process.env.VUE_APP_API_SERVER_URL}${this.versionEndPoint}`;
        console.log(`Loading version number from ${url}`);
        var response = await axios.get(url)
        return response.data;
      }
}

export const versionService = new VersionService();
