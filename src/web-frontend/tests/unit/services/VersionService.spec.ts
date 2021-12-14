import VersionService, { versionService } from "@/services/VersionService";
import axios, { AxiosResponse } from 'axios';
import VersionInfo from '@/models/VersionInfo';

jest.mock('axios');

afterEach(() => {
  jest.clearAllMocks();
});

describe("getVersion", () => {

  describe("when API call is made", () => {

    it("executes using the version url", async () => {

      (axios.get as jest.Mock).mockImplementationOnce(() => Promise.resolve({ data: new VersionInfo("0.0.0") }));
      let baseUrl = 'http://test.apotheca.com';
      process.env.VUE_APP_BASEURL = baseUrl;
      let vs = new VersionService();

      let expectedVersionUrl = `${baseUrl}${vs.versionEndPpoint}`;

      await vs.getVersion();
      expect(axios.get).toHaveBeenCalledTimes(1);
      expect(axios.get).toHaveBeenCalledWith(expectedVersionUrl);

    });

  });

  describe("when API call is succcessful", () => {
    
    it("should return the version number", async () => {

      let mockVersionInfo = new VersionInfo("2.3.4");

      (axios.get as jest.Mock).mockImplementationOnce(() => Promise.resolve({ data: mockVersionInfo }));

      let result = await versionService.getVersion();

      expect(axios.get).toHaveBeenCalledTimes(1);
      expect(result.data).toEqual(mockVersionInfo);

    });
  });

});
